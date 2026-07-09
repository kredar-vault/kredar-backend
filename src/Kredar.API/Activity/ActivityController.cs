using Kredar.API.Common;
using Kredar.API.Data;
using Kredar.API.Transactions;
using Kredar.API.Transfers;
using Kredar.API.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Activity;

[ApiController]
[Authorize]
[Route("api/v1/activity")]
public class ActivityController(AppDbContext db) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet]
    public async Task<IActionResult> Feed([FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;

        var txEvents = await db.Transactions
            .Where(t => t.TenantId == TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(200)
            .Select(t => new ActivityEvent
            {
                Id = t.Id.ToString(),
                Type = "pay_in",
                Title = "Payment received",
                Description = t.Narration ?? $"₦{t.Amount:N0} via {t.PaymentMethod}",
                AmountNaira = t.Amount,
                Status = t.Status.ToString(),
                Reference = t.Reference,
                OccurredAt = t.CreatedAt,
            })
            .ToListAsync(ct);

        var trEvents = await db.Transfers
            .Where(t => t.TenantId == TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(200)
            .Select(t => new ActivityEvent
            {
                Id = t.Id.ToString(),
                Type = "pay_out",
                Title = "Transfer sent",
                Description = t.Narration ?? $"₦{t.Amount:N0} transfer",
                AmountNaira = t.Amount,
                Status = t.Status.ToString(),
                Reference = t.MerchantTxRef,
                OccurredAt = t.CreatedAt,
            })
            .ToListAsync(ct);

        var whEvents = await db.WebhookDeliveries
            .Where(d => d.TenantId == TenantId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(100)
            .Select(d => new ActivityEvent
            {
                Id = d.Id.ToString(),
                Type = "webhook",
                Title = "Webhook fired",
                Description = d.EventType,
                Status = d.Status.ToString(),
                OccurredAt = d.CreatedAt,
            })
            .ToListAsync(ct);

        var all = txEvents.Concat(trEvents).Concat(whEvents)
            .OrderByDescending(e => e.OccurredAt)
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        var total = txEvents.Count + trEvents.Count + whEvents.Count;

        return Ok(ApiResponse<object>.Success(new { total, page, pageSize, items = all }));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var txCount = await db.Transactions.CountAsync(t => t.TenantId == TenantId, ct);
        var trCount = await db.Transfers.CountAsync(t => t.TenantId == TenantId, ct);
        var whCount = await db.WebhookDeliveries.CountAsync(d => d.TenantId == TenantId, ct);

        var since7d = DateTime.UtcNow.AddDays(-7);
        var txRecent = await db.Transactions.CountAsync(t => t.TenantId == TenantId && t.CreatedAt >= since7d, ct);
        var trRecent = await db.Transfers.CountAsync(t => t.TenantId == TenantId && t.CreatedAt >= since7d, ct);
        var whRecent = await db.WebhookDeliveries.CountAsync(d => d.TenantId == TenantId && d.CreatedAt >= since7d, ct);

        return Ok(ApiResponse<object>.Success(new
        {
            total = txCount + trCount + whCount,
            payIns = txCount,
            payOuts = trCount,
            webhookEvents = whCount,
            last7Days = new { payIns = txRecent, payOuts = trRecent, webhookEvents = whRecent },
        }));
    }
}

public class ActivityEvent
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? AmountNaira { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime OccurredAt { get; set; }
}
