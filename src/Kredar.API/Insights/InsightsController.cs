using Kredar.API.Common;
using Kredar.API.Insights.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Insights;

[ApiController]
[Route("api/v1/insights")]
[Authorize]
public class InsightsController(InsightsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetAsync(tenantId, ct);
        return Ok(ApiResponse<InsightsResponse>.Success(result));
    }

    [HttpGet("balance")]
    public async Task<IActionResult> Balance(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var balance = await service.GetBalanceAsync(tenantId, ct);
        return Ok(ApiResponse<BalanceResponse>.Success(balance));
    }
}
