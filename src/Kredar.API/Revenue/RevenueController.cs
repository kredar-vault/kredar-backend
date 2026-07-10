using Kredar.API.Common;
using Kredar.API.Revenue.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Revenue;

[ApiController]
[Route("api/v1/revenue")]
[Authorize]
public class RevenueController(RevenueService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetAsync(tenantId, ct);
        return Ok(ApiResponse<RevenueResponse>.Success(result));
    }
}
