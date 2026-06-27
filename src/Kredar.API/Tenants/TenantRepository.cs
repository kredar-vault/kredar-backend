using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Tenants;

public class TenantRepository(AppDbContext db)
{
    public async Task<Tenant?> FindByEmailAsync(string email) =>
        await db.Tenants.FirstOrDefaultAsync(t => t.Email == email);

    public async Task<Tenant?> FindByIdAsync(Guid id) =>
        await db.Tenants.FindAsync(id);

    public async Task AddAsync(Tenant tenant)
    {
        await db.Tenants.AddAsync(tenant);
        await db.SaveChangesAsync();
    }
}
