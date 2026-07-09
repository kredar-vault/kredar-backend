using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Webhooks;

public class WebhookEndpointRepository(AppDbContext db)
{
    public async Task<List<WebhookEndpoint>> GetAllAsync(Guid tenantId) =>
        await db.WebhookEndpoints
            .Where(e => e.TenantId == tenantId && e.Active)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

    public async Task<List<WebhookEndpoint>> GetActiveByTenantAsync(Guid tenantId) =>
        await db.WebhookEndpoints
            .Where(e => e.TenantId == tenantId && e.Active)
            .ToListAsync();

    public async Task<WebhookEndpoint?> FindByIdAsync(Guid tenantId, Guid id) =>
        await db.WebhookEndpoints
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id);

    public async Task AddAsync(WebhookEndpoint endpoint)
    {
        db.WebhookEndpoints.Add(endpoint);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(WebhookEndpoint endpoint)
    {
        db.WebhookEndpoints.Update(endpoint);
        await db.SaveChangesAsync();
    }
}
