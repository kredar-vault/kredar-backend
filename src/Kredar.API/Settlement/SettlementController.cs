using Kredar.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Settlement;

public class UpdateSettlementRequest
{
    public string? SettlementAccountNumber { get; set; }
    public string? SettlementBankCode { get; set; }
    public string? SettlementAccountName { get; set; }
    public bool AutoSettle { get; set; }
    public decimal MinPayoutNaira { get; set; }
}

public class SetSplitsRequest
{
    public List<SplitLegRequest> Splits { get; set; } = [];
}

public class SplitLegRequest
{
    public string BeneficiaryName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string Basis { get; set; } = "Percentage";
    public int ShareBps { get; set; }
    public decimal FlatNaira { get; set; }
}

public class EscrowHoldRequest
{
    public string? ReleaseCondition { get; set; }
}

[ApiController]
[Route("api/v1/settings/settlement")]
[Authorize]
public class SettlementConfigController(SettlementService settlement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var config = await settlement.GetConfigAsync(tenantId, ct);
        return Ok(ApiResponse<SettlementConfig>.Success(config));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettlementRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var config = await settlement.UpdateConfigAsync(tenantId,
            request.SettlementAccountNumber, request.SettlementBankCode, request.SettlementAccountName,
            request.AutoSettle, request.MinPayoutNaira, ct);
        return Ok(ApiResponse<SettlementConfig>.Success(config));
    }
}

[ApiController]
[Route("api/v1/settings/splits")]
[Authorize]
public class SettlementSplitsController(SettlementService settlement) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var splits = await settlement.GetSplitsAsync(tenantId, ct);
        return Ok(ApiResponse<List<SettlementSplit>>.Success(splits));
    }

    [HttpPut]
    public async Task<IActionResult> Set([FromBody] SetSplitsRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var specs = request.Splits.Select(s => new SplitSpec(s.BeneficiaryName, s.AccountNumber, s.BankCode, s.Basis, s.ShareBps, s.FlatNaira)).ToList();
        var splits = await settlement.SetSplitsAsync(tenantId, specs, ct);
        return Ok(ApiResponse<List<SettlementSplit>>.Success(splits));
    }
}

[ApiController]
[Route("api/v1/settlements")]
[Authorize]
public class EscrowController(SettlementService settlement) : ControllerBase
{
    [HttpPost("{accountReference}/hold")]
    public async Task<IActionResult> Hold(string accountReference, [FromBody] EscrowHoldRequest? request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var hold = await settlement.HoldAsync(tenantId, accountReference, request?.ReleaseCondition, ct);
        return Ok(ApiResponse<EscrowHold>.Success(hold));
    }

    [HttpPost("{accountReference}/release")]
    public async Task<IActionResult> Release(string accountReference, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        await settlement.ReleaseAsync(tenantId, accountReference, ct);
        return Ok(ApiResponse<object>.Success(new { }, "Escrow hold released."));
    }
}
