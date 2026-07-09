using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Notifications;

public class NotificationService(AppDbContext db)
{
    public async Task CreateAsync(Guid tenantId, NotificationType type, string title, string message, string? metadata = null)
    {
        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = type,
            Title = title,
            Message = message,
            Metadata = metadata
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetAllAsync(Guid tenantId, bool? unreadOnly, int take, CancellationToken ct)
    {
        var query = db.Notifications.Where(n => n.TenantId == tenantId);
        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);
        return await query.OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid tenantId, CancellationToken ct) =>
        await db.Notifications.CountAsync(n => n.TenantId == tenantId && !n.IsRead, ct);

    public async Task MarkReadAsync(Guid tenantId, IEnumerable<Guid>? ids, CancellationToken ct)
    {
        var query = db.Notifications.Where(n => n.TenantId == tenantId && !n.IsRead);
        if (ids != null)
        {
            var idList = ids.ToList();
            if (idList.Count > 0)
                query = query.Where(n => idList.Contains(n.Id));
        }
        await query.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
