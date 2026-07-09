using Kredar.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Notifications;

public record MarkReadRequest(List<Guid>? Ids);

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
[Tags("Notifications")]
public class NotificationsController(NotificationService notifications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? unread, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var items = await notifications.GetAllAsync(tenantId, unread, take, ct);
        var unreadCount = await notifications.GetUnreadCountAsync(tenantId, ct);
        return Ok(ApiResponse<object>.Success(new { items, unreadCount }));
    }

    [HttpPatch("read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest? request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        await notifications.MarkReadAsync(tenantId, request?.Ids, ct);
        return Ok(ApiResponse<string>.Success("Notifications marked as read."));
    }
}
