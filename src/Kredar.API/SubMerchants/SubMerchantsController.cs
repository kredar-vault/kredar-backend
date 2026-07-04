using Kredar.API.Common;
using Kredar.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.SubMerchants;

public class CreateSubMerchantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public class SetSubMerchantPayoutRequest
{
    public string BankName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int PlatformFeeBps { get; set; } = 0;
}

[ApiController]
[Route("api/v1/sub-merchants")]
[Authorize]
public class SubMerchantsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubMerchantRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);

        var exists = await db.SubMerchants.AnyAsync(s => s.TenantId == tenantId && s.Reference == request.Reference, ct);
        if (exists) throw new Exception($"A sub-merchant with reference '{request.Reference}' already exists.");

        var sub = new SubMerchant
        {
            TenantId = tenantId,
            Name = request.Name,
            Reference = request.Reference
        };

        db.SubMerchants.Add(sub);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<SubMerchant>.Success(sub, "Sub-merchant created."));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var subs = await db.SubMerchants.Where(s => s.TenantId == tenantId).OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
        return Ok(ApiResponse<List<SubMerchant>>.Success(subs));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var sub = await db.SubMerchants.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct)
            ?? throw new Exception("Sub-merchant not found.");
        return Ok(ApiResponse<SubMerchant>.Success(sub));
    }

    [HttpPut("{id:guid}/payout")]
    public async Task<IActionResult> SetPayout(Guid id, [FromBody] SetSubMerchantPayoutRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var sub = await db.SubMerchants.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct)
            ?? throw new Exception("Sub-merchant not found.");

        sub.SettlementBankName = request.BankName;
        sub.SettlementBankCode = request.BankCode;
        sub.SettlementAccountNumber = request.AccountNumber;
        sub.SettlementAccountName = request.AccountName;
        sub.PlatformFeeBps = request.PlatformFeeBps;
        db.SubMerchants.Update(sub);
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse<SubMerchant>.Success(sub, "Payout account updated."));
    }
}
