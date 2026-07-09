using Kredar.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.ApiKeys;

public class CreateApiKeyRequest
{
    public string Label { get; set; } = string.Empty;
    public string Mode { get; set; } = "test";
}

public record ApiKeyResponse(Guid Id, string ClientId, string? ClientSecret, string Mode, string Label, string Status, DateTime? LastUsedAt, DateTime CreatedAt);

[ApiController]
[Route("api/v1/api-keys")]
[Authorize]
public class ApiKeysController(ApiKeyService apiKeys) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var mode = request.Mode.Equals("live", StringComparison.OrdinalIgnoreCase) ? ApiKeyMode.Live : ApiKeyMode.Test;
        var created = await apiKeys.CreateAsync(tenantId, request.Label, mode);
        var response = new ApiKeyResponse(created.Id, created.ClientId, created.ClientSecret, created.Mode, created.Label, "Active", null, created.CreatedAt);
        return Ok(ApiResponse<ApiKeyResponse>.Success(response, "API key created. Save the secret — it won't be shown again."));
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var keys = await apiKeys.ListAsync(tenantId);
        var response = keys.Select(k => new ApiKeyResponse(k.Id, k.ClientId, null, k.Mode.ToString(), k.Label, k.Status.ToString(), k.LastUsedAt, k.CreatedAt));
        return Ok(ApiResponse<IEnumerable<ApiKeyResponse>>.Success(response));
    }

    [HttpPost("{id:guid}/rotate")]
    public async Task<IActionResult> Rotate(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var created = await apiKeys.RotateAsync(id, tenantId);
        var response = new ApiKeyResponse(created.Id, created.ClientId, created.ClientSecret, created.Mode, created.Label, "Active", null, created.CreatedAt);
        return Ok(ApiResponse<ApiKeyResponse>.Success(response, "Key rotated. Save the new secret — it won't be shown again."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        await apiKeys.RevokeAsync(id, tenantId);
        return Ok(ApiResponse<object>.Success(new { }, "API key revoked."));
    }
}
