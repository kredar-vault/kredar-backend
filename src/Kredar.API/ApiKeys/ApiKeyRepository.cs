using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.ApiKeys;

public class ApiKeyRepository(AppDbContext db)
{
    public async Task<List<ApiKey>> GetByTenantAsync(Guid tenantId) =>
        await db.ApiKeys.Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt).ToListAsync();

    public async Task<ApiKey?> FindByIdAsync(Guid id, Guid tenantId) =>
        await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId);

    public async Task AddAsync(ApiKey key)
    {
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ApiKey key)
    {
        db.ApiKeys.Update(key);
        await db.SaveChangesAsync();
    }
}
