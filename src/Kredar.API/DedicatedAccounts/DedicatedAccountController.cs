using Kredar.API.Common;
using Kredar.API.DedicatedAccounts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.DedicatedAccounts;

[ApiController]
[Route("api/v1/dedicated-accounts")]
[Authorize]
public class DedicatedAccountController(DedicatedAccountService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDedicatedAccountRequest req)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.CreateAsync(tenantId, req);
        return Ok(ApiResponse<DedicatedAccountResponse>.Success(result, "Dedicated account created."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetAllAsync(tenantId);
        return Ok(ApiResponse<List<DedicatedAccountResponse>>.Success(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await service.GetByIdAsync(tenantId, id);
        return Ok(ApiResponse<DedicatedAccountResponse>.Success(result));
    }
}
