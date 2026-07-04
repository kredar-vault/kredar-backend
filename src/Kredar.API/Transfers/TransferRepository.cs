using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Transfers;

public class TransferRepository(AppDbContext db)
{
    public async Task<List<Transfer>> GetAllAsync(Guid tenantId) =>
        await db.Transfers
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<Transfer?> FindByRefAsync(Guid tenantId, string merchantTxRef) =>
        await db.Transfers
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.MerchantTxRef == merchantTxRef);

    public async Task AddAsync(Transfer transfer)
    {
        db.Transfers.Add(transfer);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transfer transfer)
    {
        db.Transfers.Update(transfer);
        await db.SaveChangesAsync();
    }
}
