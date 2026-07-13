using Kredar.API.Config;
using Kredar.API.Insights;
using Kredar.API.Nomba;
using Kredar.API.Notifications;
using Kredar.API.Transfers.Dto;
using Microsoft.Extensions.Options;

namespace Kredar.API.Transfers;

public class TransferService(
    TransferRepository repo,
    NombaClient nombaClient,
    NotificationService notif,
    InsightsService insightsService,
    IOptions<NombaSettings> nombaOpts)
{
    private readonly bool _simulateTransfers = nombaOpts.Value.SimulateTransfers;
    public async Task<BankLookupResponse> LookupAsync(string accountNumber, string bankCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(bankCode))
            throw new ArgumentException("accountNumber and bankCode are required.");

        var result = await nombaClient.LookupBankAccountAsync(accountNumber.Trim(), bankCode.Trim(), ct);
        return new BankLookupResponse
        {
            AccountName = result.AccountName,
            AccountNumber = result.AccountNumber,
            BankCode = result.BankCode,
        };
    }

    public async Task<TransferResponse> InitiateAsync(Guid tenantId, CreateTransferRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.MerchantTxRef))
            throw new ArgumentException("MerchantTxRef is required.");
        if (req.Amount <= 0)
            throw new ArgumentException("Amount must be positive.");

        var reference = req.MerchantTxRef.Trim();

        var balance = await insightsService.GetBalanceAsync(tenantId, ct);
        if (req.Amount > balance.AvailableBalance)
            throw new ArgumentException($"Insufficient balance. Available: ₦{balance.AvailableBalance:N2}");

        var existing = await repo.FindByRefAsync(tenantId, reference);
        if (existing is not null)
            return Map(existing);

        var transfer = new Transfer
        {
            TenantId = tenantId,
            MerchantTxRef = reference,
            Amount = req.Amount,
            RecipientAccountNumber = req.AccountNumber.Trim(),
            RecipientBankCode = req.BankCode.Trim(),
            Narration = req.Narration,
        };

        await repo.AddAsync(transfer);

        string? recipientName = null;
        try
        {
            var lookup = await nombaClient.LookupBankAccountAsync(req.AccountNumber.Trim(), req.BankCode.Trim(), ct);
            recipientName = lookup.AccountName;
            transfer.RecipientName = recipientName;
            await repo.UpdateAsync(transfer);
        }
        catch { }

        if (_simulateTransfers)
        {
            transfer.Status = TransferStatus.Succeeded;
            transfer.ProviderReference = "sim-" + Guid.NewGuid().ToString("N")[..12];
            transfer.CompletedAt = DateTime.UtcNow;
            await repo.UpdateAsync(transfer);
            await notif.CreateAsync(tenantId, NotificationType.TransferCompleted,
                "Transfer successful",
                $"₦{req.Amount:N2} sent to {recipientName ?? req.AccountNumber}. Ref: {reference}.");
            return Map(transfer);
        }

        var result = await nombaClient.InitiateTransferAsync(
            reference, req.Amount, req.AccountNumber.Trim(), req.BankCode.Trim(), recipientName, req.Narration, ct);

        if (result.Success)
        {
            transfer.Status = TransferStatus.Succeeded;
            transfer.ProviderReference = result.Reference;
            transfer.CompletedAt = DateTime.UtcNow;
        }
        else if (result.IsPending)
        {
            transfer.Status = TransferStatus.Pending;
            transfer.ProviderReference = result.Reference;
        }
        else
        {
            transfer.Status = TransferStatus.Failed;
            transfer.FailureReason = result.Error;
            transfer.CompletedAt = DateTime.UtcNow;
        }

        await repo.UpdateAsync(transfer);

        if (result.Success)
            await notif.CreateAsync(tenantId, NotificationType.TransferCompleted,
                "Transfer successful",
                $"₦{req.Amount:N2} sent to {recipientName ?? req.AccountNumber}. Ref: {reference}.");
        else if (result.IsPending)
            await notif.CreateAsync(tenantId, NotificationType.TransferCompleted,
                "Transfer processing",
                $"₦{req.Amount:N2} transfer to {recipientName ?? req.AccountNumber} is being processed. Ref: {reference}.");
        else
            await notif.CreateAsync(tenantId, NotificationType.TransferFailed,
                "Transfer failed",
                $"₦{req.Amount:N2} transfer to {recipientName ?? req.AccountNumber} failed. Reason: {result.Error}. Ref: {reference}.");
        return Map(transfer);
    }

    public async Task<TransferResponse> GetAsync(Guid tenantId, string merchantTxRef)
    {
        var transfer = await repo.FindByRefAsync(tenantId, merchantTxRef.Trim())
            ?? throw new KeyNotFoundException($"No transfer with ref '{merchantTxRef}'.");
        return Map(transfer);
    }

    public async Task<List<TransferResponse>> GetAllAsync(Guid tenantId)
    {
        var transfers = await repo.GetAllAsync(tenantId);
        return transfers.Select(Map).ToList();
    }

    private static TransferResponse Map(Transfer t) => new()
    {
        Id = t.Id,
        MerchantTxRef = t.MerchantTxRef,
        Amount = t.Amount,
        RecipientAccountNumber = t.RecipientAccountNumber,
        RecipientBankCode = t.RecipientBankCode,
        RecipientName = t.RecipientName,
        Narration = t.Narration,
        Status = t.Status.ToString(),
        ProviderReference = t.ProviderReference,
        FailureReason = t.FailureReason,
        CreatedAt = t.CreatedAt,
        CompletedAt = t.CompletedAt,
    };
}
