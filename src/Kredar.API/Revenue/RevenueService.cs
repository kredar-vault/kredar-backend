using Kredar.API.Data;
using Kredar.API.Revenue.Dto;
using Kredar.API.Tenants;
using Kredar.API.Transfers;
using Kredar.API.Transfers.Dto;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Revenue;

public class RevenueService(AppDbContext db, TransferService transferService)
{
    public async Task<RevenueResponse> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await db.Set<Tenant>().FindAsync([tenantId], ct);
        var businessType = (tenant?.BusinessType ?? "").ToUpperInvariant();
        var isPlatform = businessType.Contains("PLATFORM");

        if (!isPlatform)
            return new RevenueResponse { Enabled = false };

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var txns = db.Transactions.Where(t => t.TenantId == tenantId);

        var totalRevenue = await txns.SumAsync(t => (decimal?)t.Fee, ct) ?? 0;

        var todayRevenue = await txns
            .Where(t => t.CreatedAt >= today)
            .SumAsync(t => (decimal?)t.Fee, ct) ?? 0;

        var monthlyRevenue = await txns
            .Where(t => t.CreatedAt >= monthStart)
            .SumAsync(t => (decimal?)t.Fee, ct) ?? 0;

        // Available revenue = fees earned minus transfers already made
        var totalWithdrawn = await db.Transfers
            .Where(t => t.TenantId == tenantId && t.Status == TransferStatus.Succeeded)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var availableRevenue = Math.Max(0, totalRevenue - totalWithdrawn);

        return new RevenueResponse
        {
            Enabled = true,
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            MonthlyRevenue = monthlyRevenue,
            AvailableRevenue = availableRevenue,
        };
    }

    public async Task<object> WithdrawAsync(Guid tenantId, RevenueWithdrawRequest req, CancellationToken ct = default)
    {
        var tenant = await db.Set<Tenant>().FindAsync([tenantId], ct);
        var businessType = (tenant?.BusinessType ?? "").ToUpperInvariant();

        if (!businessType.Contains("PLATFORM"))
            throw new InvalidOperationException(
                "Only platform accounts can withdraw revenue. " +
                "Merchant accounts should use POST /api/v1/balance/withdraw.");

        var revenue = await GetAsync(tenantId, ct);

        if (req.Amount <= 0)
            throw new InvalidOperationException("Withdrawal amount must be greater than 0.");

        if (req.Amount > revenue.AvailableRevenue)
            throw new InvalidOperationException(
                $"Insufficient revenue. Available to withdraw: ₦{revenue.AvailableRevenue:N2}.");

        var transferReq = new CreateTransferRequest
        {
            MerchantTxRef = $"rev_{Guid.NewGuid():N}",
            Amount = req.Amount,
            AccountNumber = req.AccountNumber,
            BankCode = req.BankCode,
            Narration = req.Narration,
        };

        return await transferService.InitiateAsync(tenantId, transferReq, ct);
    }
}
