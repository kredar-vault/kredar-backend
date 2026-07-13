using Kredar.API.Data;
using Kredar.API.DedicatedAccounts;
using Kredar.API.Insights.Dto;
using Kredar.API.Transactions;
using Kredar.API.Transfers;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Insights;

public class InsightsService(AppDbContext db)
{
    public async Task<InsightsResponse> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var accounts = db.DedicatedAccounts.Where(a => a.TenantId == tenantId);
        var txns = db.Transactions.Where(t => t.TenantId == tenantId);
        var transfers = db.Transfers.Where(t => t.TenantId == tenantId);

        var vaCount = await accounts.CountAsync(ct);
        var totalExpected = await accounts.SumAsync(a => (decimal?)a.ExpectedAmount, ct) ?? 0;
        var totalPaid = await accounts.SumAsync(a => (decimal?)a.AmountPaid, ct) ?? 0;
        var deficit = await accounts
            .Where(a => a.ExpectedAmount != null && a.AmountPaid < a.ExpectedAmount)
            .SumAsync(a => (decimal?)(a.ExpectedAmount - a.AmountPaid), ct) ?? 0;
        var fullyPaid = await accounts.CountAsync(a => a.PaymentState == PaymentState.FullyPaid, ct);
        var partiallyPaid = await accounts.CountAsync(a => a.PaymentState == PaymentState.PartiallyPaid, ct);

        var txnCount = await txns.CountAsync(ct);
        var reconciled = await txns.CountAsync(t => t.Status == TransactionStatus.Reconciled, ct);
        var underpaid = await txns.CountAsync(t => t.Status == TransactionStatus.Underpaid, ct);
        var overpaid = await txns.CountAsync(t => t.Status == TransactionStatus.Overpaid, ct);
        var reversed = await txns.CountAsync(t => t.Status == TransactionStatus.Reversed, ct);

        var transferCount = await transfers.CountAsync(ct);
        var totalTransferred = await transfers
            .Where(t => t.Status == TransferStatus.Succeeded)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var totalFees = await txns.SumAsync(t => (decimal?)t.Fee, ct) ?? 0;
        var rate = totalExpected > 0 ? Math.Round((double)(totalPaid / totalExpected) * 100, 1) : 0;

        return new InsightsResponse
        {
            DedicatedAccounts = vaCount,
            AvailableBalance = totalPaid - totalFees - totalTransferred,
            TotalFees = totalFees,
            TotalTransactions = txnCount,
            TotalCollected = totalPaid,
            TotalExpected = totalExpected,
            OutstandingDeficit = deficit,
            CollectionRatePct = rate,
            Reconciled = reconciled,
            Underpaid = underpaid,
            Overpaid = overpaid,
            Reversed = reversed,
            FullyPaidAccounts = fullyPaid,
            PartiallyPaidAccounts = partiallyPaid,
            TotalTransfers = transferCount,
            TotalTransferred = totalTransferred,
        };
    }

    public async Task<BalanceResponse> GetBalanceAsync(Guid tenantId, CancellationToken ct = default)
    {
        var totalCollected = await db.DedicatedAccounts
            .Where(a => a.TenantId == tenantId)
            .SumAsync(a => (decimal?)a.AmountPaid, ct) ?? 0;

        var totalFees = await db.Transactions
            .Where(t => t.TenantId == tenantId)
            .SumAsync(t => (decimal?)t.Fee, ct) ?? 0;

        var totalTransferred = await db.Transfers
            .Where(t => t.TenantId == tenantId &&
                        (t.Status == TransferStatus.Succeeded || t.Status == TransferStatus.Pending))
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        return new BalanceResponse
        {
            AvailableBalance = totalCollected - totalFees - totalTransferred,
            TotalCollected = totalCollected,
            TotalFees = totalFees,
            TotalTransferred = totalTransferred,
            Currency = "NGN",
        };
    }
}
