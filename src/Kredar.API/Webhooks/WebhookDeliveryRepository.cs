using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Webhooks;

public class WebhookDeliveryRepository(AppDbContext db)
{
    public async Task<List<WebhookDelivery>> GetDueAsync(int batchSize) =>
        await db.WebhookDeliveries
            .Where(d => (d.Status == WebhookDeliveryStatus.Pending || d.Status == WebhookDeliveryStatus.Failed)
                        && d.NextAttemptAt != null && d.NextAttemptAt <= DateTime.UtcNow)
            .OrderBy(d => d.NextAttemptAt)
            .Take(batchSize)
            .ToListAsync();

    public async Task<List<WebhookDelivery>> GetByTenantAsync(Guid tenantId, WebhookDeliveryStatus? status = null)
    {
        var q = db.WebhookDeliveries.Where(d => d.TenantId == tenantId);
        if (status.HasValue) q = q.Where(d => d.Status == status.Value);
        return await q.OrderByDescending(d => d.CreatedAt).Take(200).ToListAsync();
    }

    public async Task<WebhookDelivery?> FindByIdAsync(Guid tenantId, Guid id) =>
        await db.WebhookDeliveries.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id);

    public async Task UpdateAsync(WebhookDelivery delivery)
    {
        db.WebhookDeliveries.Update(delivery);
        await db.SaveChangesAsync();
    }
}
