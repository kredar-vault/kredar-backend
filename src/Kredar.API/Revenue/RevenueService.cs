using Kredar.API.Data;
using Kredar.API.Revenue.Dto;
using Kredar.API.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Revenue;

public class RevenueService(AppDbContext db)
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

        return new RevenueResponse
        {
            Enabled = true,
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            MonthlyRevenue = monthlyRevenue,
        };
    }
}
