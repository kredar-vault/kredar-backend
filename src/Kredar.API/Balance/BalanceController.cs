using Kredar.API.Balance.Dto;
using Kredar.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Balance;

[ApiController]
[Route("api/v1/balance")]
[Authorize]
public class BalanceController(BalanceService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetFullBalanceAsync(tenantId, ct);
        return Ok(ApiResponse<FullBalanceResponse>.Success(result));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> Activity(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetActivityAsync(tenantId, ct);
        return Ok(ApiResponse<List<BalanceActivityItem>>.Success(result));
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest req, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.WithdrawAsync(tenantId, req, ct);
        return Ok(ApiResponse<object>.Success(result, "Withdrawal initiated successfully."));
    }
}
