using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Customers;

public class CustomerRepository(AppDbContext db)
{
    public async Task<List<Customer>> GetAllAsync(Guid tenantId) =>
        await db.Customers
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<List<Customer>> GetByStatusAsync(Guid tenantId, CustomerStatus status) =>
        await db.Customers
            .Where(c => c.TenantId == tenantId && c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<Customer?> FindByIdAsync(Guid tenantId, Guid customerId) =>
        await db.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == customerId);

    public async Task<Customer?> FindByEmailAsync(Guid tenantId, string email) =>
        await db.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email == email);

    public async Task AddAsync(Customer customer)
    {
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        db.Customers.Update(customer);
        await db.SaveChangesAsync();
    }

    public async Task<int> CountAsync(Guid tenantId) =>
        await db.Customers.CountAsync(c => c.TenantId == tenantId);

    public async Task<int> CountByStatusAsync(Guid tenantId, CustomerStatus status) =>
        await db.Customers.CountAsync(c => c.TenantId == tenantId && c.Status == status);
}
