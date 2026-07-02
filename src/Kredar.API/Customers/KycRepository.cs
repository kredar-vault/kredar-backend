using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Customers;

public class KycRepository(AppDbContext db)
{
    public async Task<List<CustomerKycDocument>> GetByCustomerAsync(Guid tenantId, Guid customerId) =>
        await db.CustomerKycDocuments
            .Where(k => k.TenantId == tenantId && k.CustomerId == customerId)
            .OrderByDescending(k => k.SubmittedAt)
            .ToListAsync();

    public async Task<CustomerKycDocument?> FindByIdAsync(Guid tenantId, Guid docId) =>
        await db.CustomerKycDocuments
            .FirstOrDefaultAsync(k => k.TenantId == tenantId && k.Id == docId);

    public async Task AddAsync(CustomerKycDocument doc)
    {
        db.CustomerKycDocuments.Add(doc);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerKycDocument doc)
    {
        db.CustomerKycDocuments.Update(doc);
        await db.SaveChangesAsync();
    }

    public async Task<bool> AllVerifiedAsync(Guid tenantId, Guid customerId) =>
        await db.CustomerKycDocuments
            .Where(k => k.TenantId == tenantId && k.CustomerId == customerId)
            .AllAsync(k => k.Status == KycDocumentStatus.Verified);
}
