namespace Kredar.API.Common;

public static class TenantContext
{
    public static Guid GetTenantId(HttpContext context)
    {
        var claim = context.User.FindFirst("tenantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException("Tenant not found.");
    }
}
