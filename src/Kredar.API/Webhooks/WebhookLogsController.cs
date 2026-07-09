using Kredar.API.Common;
using Kredar.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Webhooks;

[ApiController]
[Authorize]
[Route("api/v1/webhooks/logs")]
public class WebhookLogsController(AppDbContext db) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var query = db.WebhookDeliveries.Where(d => d.TenantId == TenantId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new {
                d.Id, d.EventType, d.Status, d.Attempts,
                d.LastStatusCode, d.LastError, d.DeliveredAt, d.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Success(new { total, page, pageSize, items }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var delivery = await db.WebhookDeliveries
            .Where(d => d.Id == id && d.TenantId == TenantId)
            .Select(d => new {
                d.Id, d.EventType, d.Status, d.Attempts, d.PayloadJson,
                d.LastStatusCode, d.LastError, d.DeliveredAt, d.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (delivery is null) return NotFound(ApiResponse<object>.Fail("Log not found."));
        return Ok(ApiResponse<object>.Success(delivery));
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        var delivery = await db.WebhookDeliveries
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == TenantId, ct);

        if (delivery is null) return NotFound(ApiResponse<object>.Fail("Log not found."));
        delivery.Replay();
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success("Queued for retry."));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var deliveries = db.WebhookDeliveries.Where(d => d.TenantId == TenantId);
        var total = await deliveries.CountAsync(ct);
        var delivered = await deliveries.CountAsync(d => d.Status == WebhookDeliveryStatus.Delivered, ct);
        var failed = await deliveries.CountAsync(d => d.Status == WebhookDeliveryStatus.Failed || d.Status == WebhookDeliveryStatus.DeadLetter, ct);
        var pending = await deliveries.CountAsync(d => d.Status == WebhookDeliveryStatus.Pending, ct);

        return Ok(ApiResponse<object>.Success(new { total, delivered, failed, pending }));
    }
}
