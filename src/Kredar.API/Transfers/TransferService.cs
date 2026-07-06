using Kredar.API.Nomba;
using Kredar.API.Transfers.Dto;

namespace Kredar.API.Transfers;

public class TransferService(TransferRepository repo, NombaClient nombaClient)
{
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

        // Nomba rejects transfers without accountName — look it up if not provided
        string? recipientName = null;
        try
        {
            var lookup = await nombaClient.LookupBankAccountAsync(req.AccountNumber.Trim(), req.BankCode.Trim(), ct);
            recipientName = lookup.AccountName;
            transfer.RecipientName = recipientName;
            await repo.UpdateAsync(transfer);
        }
        catch { /* best-effort; transfer attempt will still fail at Nomba with a clear error */ }

        var result = await nombaClient.InitiateTransferAsync(
            reference, req.Amount, req.AccountNumber.Trim(), req.BankCode.Trim(), recipientName, req.Narration, ct);

        if (result.Success)
        {
            transfer.Status = TransferStatus.Succeeded;
            transfer.ProviderReference = result.Reference;
            transfer.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            transfer.Status = TransferStatus.Failed;
            transfer.FailureReason = result.Error;
            transfer.CompletedAt = DateTime.UtcNow;
        }

        await repo.UpdateAsync(transfer);
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
