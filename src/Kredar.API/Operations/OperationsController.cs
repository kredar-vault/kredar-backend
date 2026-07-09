using Kredar.API.Common;
using Kredar.API.Data;
using Kredar.API.DedicatedAccounts;
using Kredar.API.Transactions;
using Kredar.API.Transfers;
using Kredar.API.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Operations;

[ApiController]
[Authorize]
[Route("api/v1/operations")]
public class OperationsController(AppDbContext db) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var totalCustomers = await db.Customers.CountAsync(c => c.TenantId == TenantId, ct);
        var activeDvas = await db.DedicatedAccounts
            .CountAsync(a => a.TenantId == TenantId && a.Status == DedicatedAccountStatus.Active, ct);

        var todayVolume = await db.Transactions
            .Where(t => t.TenantId == TenantId && t.CreatedAt >= todayUtc)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var pendingTransfers = await db.Transfers
            .CountAsync(t => t.TenantId == TenantId && t.Status == TransferStatus.Pending, ct);

        var totalCollected = await db.DedicatedAccounts
            .Where(a => a.TenantId == TenantId)
            .SumAsync(a => (decimal?)a.AmountPaid, ct) ?? 0;

        var totalFees = await db.Transactions
            .Where(t => t.TenantId == TenantId)
            .SumAsync(t => (decimal?)t.Fee, ct) ?? 0;

        var totalTransferred = await db.Transfers
            .Where(t => t.TenantId == TenantId && t.Status == TransferStatus.Succeeded)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        var settlementBalance = totalCollected - totalFees - totalTransferred;

        return Ok(ApiResponse<object>.Success(new
        {
            totalCustomers,
            activeDvas,
            todayVolume,
            pendingTransfers,
            settlementBalance,
            currency = "NGN",
        }));
    }

    [HttpGet("recent")]
    public async Task<IActionResult> Recent(CancellationToken ct)
    {
        var txns = await db.Transactions
            .Where(t => t.TenantId == TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new { type = "transaction", t.Id, t.Amount, t.Status, t.Narration, t.CreatedAt })
            .ToListAsync(ct);

        var transfers = await db.Transfers
            .Where(t => t.TenantId == TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new { type = "transfer", t.Id, t.Amount, Status = t.Status.ToString(), Narration = t.Narration, t.CreatedAt })
            .ToListAsync(ct);

        var combined = txns.Cast<object>().Concat(transfers.Cast<object>())
            .OrderByDescending(x => x is { } obj
                ? (DateTime)obj.GetType().GetProperty("CreatedAt")!.GetValue(obj)!
                : DateTime.MinValue)
            .Take(15)
            .ToList();

        return Ok(ApiResponse<object>.Success(combined));
    }
}
