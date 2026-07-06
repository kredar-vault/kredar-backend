using System.Text.Json;
using Kredar.API.Data;
using Kredar.API.DedicatedAccounts;
using Kredar.API.Nomba;
using Kredar.API.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Webhooks;

public sealed record NombaParsedEvent(
    string NombaReference,
    string AccountNumber,
    long AmountKobo,
    long FeeKobo,
    string? TransferName,
    string EventType,
    bool IsReversal);

public class NombaWebhookService(
    AppDbContext db,
    NombaSignatureVerifier signatureVerifier,
    ILogger<NombaWebhookService> logger)
{
    public async Task<bool> ProcessAsync(byte[] rawBody, string? signature, string? timestamp, CancellationToken ct = default)
    {
        if (!signatureVerifier.Verify(rawBody, signature, timestamp))
        {
            logger.LogWarning("Nomba webhook signature verification failed.");
            return false;
        }

        NombaParsedEvent? parsed;
        try { parsed = Parse(rawBody); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse Nomba webhook payload.");
            return true;
        }

        if (parsed is null)
        {
            logger.LogWarning("Nomba webhook: could not extract required fields from payload.");
            return true;
        }

        var account = await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.AccountNumber == parsed.AccountNumber && a.Status == DedicatedAccountStatus.Active, ct);

        if (account is null)
        {
            logger.LogWarning("Nomba webhook: no dedicated account found for NUBAN {AccountNumber}.", parsed.AccountNumber);
            return true;
        }

        var duplicate = await db.Transactions.AnyAsync(
            t => t.TenantId == account.TenantId && t.PaymentReference == parsed.NombaReference, ct);

        if (duplicate)
        {
            logger.LogInformation("Nomba webhook: duplicate reference {Ref}, skipping.", parsed.NombaReference);
            return true;
        }

        var amountNaira = parsed.AmountKobo / 100m;
        var feeNaira = parsed.FeeKobo / 100m;

        TransactionStatus status;
        if (parsed.IsReversal)
        {
            status = TransactionStatus.Reversed;
            account.AmountPaid = Math.Max(0, account.AmountPaid - amountNaira);
            if (account.ExpectedAmount.HasValue)
                account.PaymentState = account.AmountPaid <= 0 ? PaymentState.Unpaid : PaymentState.PartiallyPaid;
        }
        else
        {
            account.AmountPaid += amountNaira;
            if (account.ExpectedAmount.HasValue)
            {
                if (account.AmountPaid >= account.ExpectedAmount.Value)
                {
                    status = account.AmountPaid > account.ExpectedAmount.Value ? TransactionStatus.Overpaid : TransactionStatus.Reconciled;
                    account.PaymentState = account.AmountPaid > account.ExpectedAmount.Value ? PaymentState.Overpaid : PaymentState.FullyPaid;
                }
                else
                {
                    status = TransactionStatus.Underpaid;
                    account.PaymentState = PaymentState.PartiallyPaid;
                }
            }
            else
            {
                status = TransactionStatus.Reconciled;
                account.PaymentState = PaymentState.FullyPaid;
            }
        }

        var transaction = new Transaction
        {
            TenantId = account.TenantId,
            CustomerId = account.CustomerId,
            Reference = $"KRD-{Guid.NewGuid():N}",
            PaymentReference = parsed.NombaReference,
            DedicatedAccountNumber = parsed.AccountNumber,
            Amount = amountNaira,
            AmountReceived = amountNaira,
            Fee = feeNaira,
            ExpectedAmount = account.ExpectedAmount,
            Narration = parsed.TransferName ?? "Bank Transfer",
            Status = status,
        };

        db.Transactions.Add(transaction);
        db.DedicatedAccounts.Update(account);

        await QueueOutboundEventsAsync(account, transaction, ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Nomba webhook processed: NUBAN={AccountNumber} Ref={Ref} Status={Status}",
            parsed.AccountNumber, parsed.NombaReference, status);
        return true;
    }

    public async Task<(TransactionStatus Status, string Reference)> ReconcileForTenantAsync(Guid tenantId, NombaParsedEvent parsed, CancellationToken ct = default)
    {
        var account = await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.AccountNumber == parsed.AccountNumber
                && a.TenantId == tenantId
                && a.Status == DedicatedAccountStatus.Active, ct)
            ?? throw new KeyNotFoundException($"No active dedicated account '{parsed.AccountNumber}' found for your tenant.");

        var duplicate = await db.Transactions.AnyAsync(
            t => t.TenantId == tenantId && t.PaymentReference == parsed.NombaReference, ct);
        if (duplicate)
            throw new InvalidOperationException($"Reference '{parsed.NombaReference}' already reconciled.");

        return await ReconcileAsync(parsed, ct);
    }

    public async Task<(TransactionStatus Status, string Reference)> ReconcileAsync(NombaParsedEvent parsed, CancellationToken ct = default)
    {
        var account = await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.AccountNumber == parsed.AccountNumber && a.Status == DedicatedAccountStatus.Active, ct)
            ?? throw new Exception($"No active dedicated account found for account number '{parsed.AccountNumber}'.");

        var duplicate = await db.Transactions.AnyAsync(
            t => t.TenantId == account.TenantId && t.PaymentReference == parsed.NombaReference, ct);

        if (duplicate)
            throw new Exception($"Duplicate reference '{parsed.NombaReference}' — already reconciled.");

        var amountNaira = parsed.AmountKobo / 100m;
        var feeNaira = parsed.FeeKobo / 100m;

        TransactionStatus status;
        if (parsed.IsReversal)
        {
            status = TransactionStatus.Reversed;
            account.AmountPaid = Math.Max(0, account.AmountPaid - amountNaira);
            if (account.ExpectedAmount.HasValue)
                account.PaymentState = account.AmountPaid <= 0 ? PaymentState.Unpaid : PaymentState.PartiallyPaid;
        }
        else
        {
            account.AmountPaid += amountNaira;
            if (account.ExpectedAmount.HasValue)
            {
                if (account.AmountPaid >= account.ExpectedAmount.Value)
                {
                    status = account.AmountPaid > account.ExpectedAmount.Value ? TransactionStatus.Overpaid : TransactionStatus.Reconciled;
                    account.PaymentState = account.AmountPaid > account.ExpectedAmount.Value ? PaymentState.Overpaid : PaymentState.FullyPaid;
                }
                else
                {
                    status = TransactionStatus.Underpaid;
                    account.PaymentState = PaymentState.PartiallyPaid;
                }
            }
            else
            {
                status = TransactionStatus.Reconciled;
                account.PaymentState = PaymentState.FullyPaid;
            }
        }

        var transaction = new Transaction
        {
            TenantId = account.TenantId,
            CustomerId = account.CustomerId,
            Reference = $"KRD-{Guid.NewGuid():N}",
            PaymentReference = parsed.NombaReference,
            DedicatedAccountNumber = parsed.AccountNumber,
            Amount = amountNaira,
            AmountReceived = amountNaira,
            Fee = feeNaira,
            ExpectedAmount = account.ExpectedAmount,
            Narration = parsed.TransferName ?? "Simulated Deposit",
            Status = status,
        };

        db.Transactions.Add(transaction);
        db.DedicatedAccounts.Update(account);
        await QueueOutboundEventsAsync(account, transaction, ct);
        await db.SaveChangesAsync(ct);

        return (status, transaction.Reference);
    }

    private async Task QueueOutboundEventsAsync(DedicatedAccount account, Transaction txn, CancellationToken ct)
    {
        var endpoints = await db.WebhookEndpoints
            .Where(e => e.TenantId == account.TenantId && e.Active)
            .ToListAsync(ct);
        if (endpoints.Count == 0) return;

        var eventId = txn.Id.ToString("N");
        var payload = JsonSerializer.Serialize(new
        {
            id = eventId,
            @event = "deposit.reconciled",
            createdAt = DateTime.UtcNow,
            data = new
            {
                accountReference = account.Reference,
                accountNumber = account.AccountNumber,
                transactionReference = txn.PaymentReference,
                kredarReference = txn.Reference,
                amountNaira = txn.Amount,
                feeNaira = txn.Fee,
                reconciliation = txn.Status.ToString(),
                paymentState = account.PaymentState.ToString(),
                amountPaid = account.AmountPaid,
                expectedAmount = account.ExpectedAmount,
                transferName = txn.Narration,
                occurredAt = txn.CreatedAt,
            },
        });

        foreach (var endpoint in endpoints)
        {
            db.WebhookDeliveries.Add(new WebhookDelivery
            {
                TenantId = account.TenantId,
                EndpointId = endpoint.Id,
                EventId = eventId,
                EventType = "deposit.reconciled",
                PayloadJson = payload,
                Status = WebhookDeliveryStatus.Pending,
                NextAttemptAt = DateTime.UtcNow,
            });
        }
    }

    private static NombaParsedEvent? Parse(byte[] rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;

        var eventType = Str(root, "event_type") ?? Str(root, "eventType") ?? string.Empty;
        var isReversal = eventType.Contains("REVERSAL", StringComparison.OrdinalIgnoreCase)
                         || eventType.Contains("REVERSED", StringComparison.OrdinalIgnoreCase);

        var data = root.TryGetProperty("data", out var d) ? d : root;
        // Nomba may or may not nest under a "transaction" key — fall back to data itself
        var txn = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("transaction", out var t) ? t : data;
        var customer = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("customer", out var c) ? c : default;

        var nombaRef = Str(txn, "transactionId") ?? Str(txn, "transaction_id")
            ?? Str(txn, "reference") ?? Str(root, "requestId") ?? Str(root, "request_id");

        // Nomba uses aliasAccountNumber for DVA (virtual account) NUBANs
        var accountNumber = Str(txn, "aliasAccountNumber") ?? Str(txn, "alias_account_number")
            ?? Str(txn, "bankAccountNumber") ?? Str(txn, "accountNumber")
            ?? Str(txn, "destinationAccountNumber") ?? Str(customer, "accountNumber")
            ?? Str(data, "bankAccountNumber");

        if (string.IsNullOrWhiteSpace(nombaRef) || string.IsNullOrWhiteSpace(accountNumber))
            return null;

        // Nomba sends amounts in naira — always multiply by 100 to get kobo
        var amountKobo = NairaToKobo(txn, "transactionAmount") ?? NairaToKobo(txn, "amount") ?? 0;
        var feeKobo = NairaToKobo(txn, "fee") ?? NairaToKobo(txn, "transactionFee") ?? 0;

        return new NombaParsedEvent(
            nombaRef, accountNumber, amountKobo, feeKobo,
            Str(txn, "senderName") ?? Str(txn, "originatorName") ?? Str(txn, "bankAccountName"),
            eventType, isReversal);
    }

    private static long? NairaToKobo(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(name, out var a)) return null;
        if (a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var naira))
            return (long)decimal.Round(naira * 100m, 0, MidpointRounding.ToEven);
        if (a.ValueKind == JsonValueKind.String &&
            decimal.TryParse(a.GetString(), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out naira))
            return (long)decimal.Round(naira * 100m, 0, MidpointRounding.ToEven);
        return null;
    }

    private static string? Str(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;
}
