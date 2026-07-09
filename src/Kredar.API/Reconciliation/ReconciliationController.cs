using Kredar.API.Common;
using Kredar.API.Data;
using Kredar.API.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Reconciliation;

[ApiController]
[Authorize]
[Route("api/v1/reconciliation")]
public class ReconciliationController(AppDbContext db) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var txns = db.Transactions.Where(t => t.TenantId == TenantId);
        var total = await txns.CountAsync(ct);
        var matched = await txns.CountAsync(t =>
            t.Status == TransactionStatus.Reconciled || t.Status == TransactionStatus.Overpaid, ct);
        var pending = await txns.CountAsync(t =>
            t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.Underpaid, ct);
        var failed = await txns.CountAsync(t =>
            t.Status == TransactionStatus.Failed || t.Status == TransactionStatus.Reversed, ct);
        var successRate = total > 0 ? Math.Round((double)matched / total * 100, 1) : 0;

        return Ok(ApiResponse<object>.Success(new
        {
            total,
            matched,
            pendingReview = pending,
            failedMatches = failed,
            successRate,
        }));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.Transactions
            .Where(t => t.TenantId == TenantId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TransactionStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);
        if (!string.IsNullOrEmpty(customerId) && Guid.TryParse(customerId, out var cid))
            query = query.Where(t => t.CustomerId == cid);
        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Reference,
                t.PaymentReference,
                t.Amount,
                t.Fee,
                t.Currency,
                t.Status,
                t.DedicatedAccountNumber,
                t.Narration,
                t.ExpectedAmount,
                t.CustomerId,
                t.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Success(new { total, page, pageSize, items }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tx = await db.Transactions
            .Where(t => t.Id == id && t.TenantId == TenantId)
            .Select(t => new
            {
                t.Id, t.Reference, t.PaymentReference, t.Amount, t.Fee, t.Currency,
                t.Status, t.DedicatedAccountNumber, t.Narration, t.ExpectedAmount,
                t.CustomerId, t.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

        if (tx is null) return NotFound(ApiResponse<object>.Fail("Transaction not found."));

        // Enrich with customer and DVA info
        object? customer = null;
        if (tx.CustomerId.HasValue)
        {
            customer = await db.Customers
                .Where(c => c.Id == tx.CustomerId.Value)
                .Select(c => new { c.Id, c.FirstName, c.LastName, c.Email, c.Status })
                .FirstOrDefaultAsync(ct);
        }

        object? dva = null;
        if (!string.IsNullOrEmpty(tx.DedicatedAccountNumber))
        {
            dva = await db.DedicatedAccounts
                .Where(a => a.AccountNumber == tx.DedicatedAccountNumber)
                .Select(a => new { a.Id, a.AccountNumber, a.AccountName, a.BankName, a.Status })
                .FirstOrDefaultAsync(ct);
        }

        return Ok(ApiResponse<object>.Success(new { transaction = tx, customer, dedicatedAccount = dva }));
    }

    [HttpPost("{id:guid}/match")]
    public async Task<IActionResult> Match(Guid id, [FromBody] MatchRequest req, CancellationToken ct)
    {
        var tx = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId, ct);
        if (tx is null) return NotFound(ApiResponse<object>.Fail("Transaction not found."));

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == req.CustomerId && c.TenantId == TenantId, ct);
        if (customer is null) return BadRequest(ApiResponse<object>.Fail("Customer not found."));

        tx.CustomerId = req.CustomerId;
        tx.Status = TransactionStatus.Reconciled;
        if (!string.IsNullOrWhiteSpace(req.Note))
            tx.Narration = req.Note;

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success("Transaction matched to customer."));
    }

    [HttpPost("{id:guid}/ignore")]
    public async Task<IActionResult> Ignore(Guid id, [FromBody] IgnoreRequest? req, CancellationToken ct)
    {
        var tx = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId, ct);
        if (tx is null) return NotFound(ApiResponse<object>.Fail("Transaction not found."));

        tx.Status = TransactionStatus.Reversed;
        if (!string.IsNullOrWhiteSpace(req?.Reason))
            tx.Narration = $"[IGNORED] {req.Reason}";

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Success("Transaction marked as ignored."));
    }
}

public class MatchRequest
{
    public Guid CustomerId { get; set; }
    public string? Note { get; set; }
}

public class IgnoreRequest
{
    public string? Reason { get; set; }
}
