using Kredar.API.Common;
using Kredar.API.Tenants.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Tenants;

[ApiController]
[Route("api/v1/tenants")]
[Authorize]
public class TenantController(TenantService tenantService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await tenantService.GetProfileAsync(tenantId);
        return Ok(ApiResponse<TenantProfileResponse>.Success(result));
    }

    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await tenantService.UpdateProfileAsync(tenantId, request);
        return Ok(ApiResponse<TenantProfileResponse>.Success(result, "Profile updated successfully."));
    }

    [HttpPatch("business-type")]
    public async Task<IActionResult> SetBusinessType([FromBody] SetBusinessTypeRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await tenantService.SetBusinessTypeAsync(tenantId, request.BusinessType);
        return Ok(ApiResponse<TenantProfileResponse>.Success(result, "Business type saved."));
    }
}
