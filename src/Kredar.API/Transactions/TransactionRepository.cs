using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Transactions;

public class TransactionRepository(AppDbContext db)
{
    public async Task<List<Transaction>> GetAllAsync(Guid tenantId, TransactionStatus? status = null)
    {
        var query = db.Transactions.Where(t => t.TenantId == tenantId);
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<List<Transaction>> GetByCustomerAsync(Guid tenantId, Guid customerId, TransactionStatus? status = null)
    {
        var query = db.Transactions.Where(t => t.TenantId == tenantId && t.CustomerId == customerId);
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<Transaction?> FindByIdAsync(Guid tenantId, Guid id) =>
        await db.Transactions.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == id);

    public async Task<decimal> SumTodayAsync(Guid tenantId, Guid? customerId = null)
    {
        var today = DateTime.UtcNow.Date;
        var query = db.Transactions.Where(t => t.TenantId == tenantId && t.CreatedAt >= today);
        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId);
        return await query.SumAsync(t => t.Amount);
    }

    public async Task<int> CountByStatusAsync(Guid tenantId, Guid? customerId, TransactionStatus status)
    {
        var query = db.Transactions.Where(t => t.TenantId == tenantId && t.Status == status);
        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId);
        return await query.CountAsync();
    }

    public async Task<int> CountExceptionsAsync(Guid tenantId, Guid? customerId = null)
    {
        var exceptionStatuses = new[] { TransactionStatus.Overpaid, TransactionStatus.Underpaid, TransactionStatus.Failed };
        var query = db.Transactions.Where(t => t.TenantId == tenantId && exceptionStatuses.Contains(t.Status));
        if (customerId.HasValue)
            query = query.Where(t => t.CustomerId == customerId);
        return await query.CountAsync();
    }

    public async Task AddAsync(Transaction transaction)
    {
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
    }
}
