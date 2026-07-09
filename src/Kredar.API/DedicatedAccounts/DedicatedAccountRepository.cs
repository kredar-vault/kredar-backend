using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.DedicatedAccounts;

public class DedicatedAccountRepository(AppDbContext db)
{
    public async Task<List<DedicatedAccount>> GetAllAsync(Guid tenantId) =>
        await db.DedicatedAccounts
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<DedicatedAccount?> FindByIdAsync(Guid tenantId, Guid id) =>
        await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == id);

    public async Task<DedicatedAccount?> FindByAccountNumberAsync(string accountNumber) =>
        await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.Status == DedicatedAccountStatus.Active);

    public async Task<DedicatedAccount?> FindByCustomerAsync(Guid tenantId, Guid customerId) =>
        await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.CustomerId == customerId);

    public async Task AddAsync(DedicatedAccount account)
    {
        db.DedicatedAccounts.Add(account);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(DedicatedAccount account)
    {
        db.DedicatedAccounts.Update(account);
        await db.SaveChangesAsync();
    }
}
