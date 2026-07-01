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

    public async Task<Tenant?> FindByVerificationTokenAsync(string token) =>
        await db.Tenants.FirstOrDefaultAsync(t => t.EmailVerificationToken == token);

    public async Task UpdateAsync(Tenant tenant)
    {
        db.Tenants.Update(tenant);
        await db.SaveChangesAsync();
    }
}
