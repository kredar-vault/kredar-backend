using Kredar.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Notifications;

/// <summary>
/// /api/v1/inbox — merchant-facing alias for notifications with inbox-style semantics.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/inbox")]
public class InboxController(NotificationService notifications) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool? unread,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var items = await notifications.GetAllAsync(TenantId, unread, take, ct);
        var unreadCount = await notifications.GetUnreadCountAsync(TenantId, ct);
        return Ok(ApiResponse<object>.Success(new { items, unreadCount }));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkOneRead(Guid id, CancellationToken ct)
    {
        await notifications.MarkReadAsync(TenantId, new List<Guid> { id }, ct);
        return Ok(ApiResponse<string>.Success("Marked as read."));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await notifications.MarkReadAsync(TenantId, null, ct);
        return Ok(ApiResponse<string>.Success("All messages marked as read."));
    }
}
