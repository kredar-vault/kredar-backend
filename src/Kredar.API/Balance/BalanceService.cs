using Kredar.API.Balance.Dto;
using Kredar.API.Data;
using Kredar.API.Tenants;
using Kredar.API.Transactions;
using Kredar.API.Transfers;
using Kredar.API.Transfers.Dto;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Balance;

public class BalanceService(AppDbContext db, TransferService transferService)
{
    public async Task<FullBalanceResponse> GetFullBalanceAsync(Guid tenantId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        var settledTxns = db.Transactions.Where(t =>
            t.TenantId == tenantId &&
            (t.Status == TransactionStatus.Reconciled ||
             t.Status == TransactionStatus.Overpaid ||
             t.Status == TransactionStatus.Underpaid));

        var totalPaid = await settledTxns.SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var totalFees = await db.Transactions
            .Where(t => t.TenantId == tenantId)
            .SumAsync(t => (decimal?)t.Fee, ct) ?? 0;

        var totalWithdrawn = await db.Transfers
            .Where(t => t.TenantId == tenantId &&
                        (t.Status == TransferStatus.Succeeded || t.Status == TransferStatus.Pending))
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var pendingBalance = await db.Transactions
            .Where(t => t.TenantId == tenantId && t.Status == TransactionStatus.Pending)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var incomingToday = await db.Transactions
            .Where(t => t.TenantId == tenantId && t.CreatedAt >= today)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var settledToday = await db.Transactions
            .Where(t => t.TenantId == tenantId &&
                        t.Status == TransactionStatus.Reconciled &&
                        t.CreatedAt >= today)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var available = Math.Max(0, totalPaid - totalFees - totalWithdrawn);

        return new FullBalanceResponse
        {
            AvailableBalance = available,
            PendingBalance = pendingBalance,
            OnHoldBalance = 0,
            IncomingToday = incomingToday,
            SettledToday = settledToday,
            Currency = "NGN",
            CanWithdraw = available > 0,
        };
    }

    public async Task<List<BalanceActivityItem>> GetActivityAsync(Guid tenantId, CancellationToken ct = default)
    {
        var credits = await db.Transactions
            .Where(t => t.TenantId == tenantId &&
                        (t.Status == TransactionStatus.Reconciled ||
                         t.Status == TransactionStatus.Overpaid))
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new BalanceActivityItem
            {
                Type = "CREDIT",
                Amount = t.Amount,
                Description = t.Narration ?? "Customer Payment",
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync(ct);

        var debits = await db.Transfers
            .Where(t => t.TenantId == tenantId &&
                        (t.Status == TransferStatus.Succeeded || t.Status == TransferStatus.Pending))
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new BalanceActivityItem
            {
                Type = "DEBIT",
                Amount = t.Amount,
                Description = t.Narration ?? $"Transfer to {t.RecipientName ?? t.RecipientAccountNumber}",
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync(ct);

        return credits
            .Concat(debits)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToList();
    }

    public async Task<object> WithdrawAsync(Guid tenantId, WithdrawRequest req, CancellationToken ct = default)
    {
        var tenant = await db.Set<Tenant>().FindAsync([tenantId], ct);
        var businessType = (tenant?.BusinessType ?? "").ToUpperInvariant();

        if (businessType.Contains("PLATFORM"))
            throw new InvalidOperationException(
                "Platform accounts cannot withdraw from the collected balance. " +
                "Use POST /api/v1/revenue/withdraw to withdraw your earned revenue.");

        var balance = await GetFullBalanceAsync(tenantId, ct);

        if (!balance.CanWithdraw)
            throw new InvalidOperationException("Withdrawals are currently unavailable.");

        if (req.Amount > balance.AvailableBalance)
            throw new InvalidOperationException(
                $"Insufficient balance. Available: ₦{balance.AvailableBalance:N2}.");

        var transferReq = new CreateTransferRequest
        {
            MerchantTxRef = $"wd_{Guid.NewGuid():N}",
            Amount = req.Amount,
            AccountNumber = req.AccountNumber,
            BankCode = req.BankCode,
            Narration = req.Narration,
        };

        return await transferService.InitiateAsync(tenantId, transferReq, ct);
    }
}
