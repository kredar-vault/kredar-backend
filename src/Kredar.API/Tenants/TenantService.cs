using Kredar.API.Tenants.Dto;

namespace Kredar.API.Tenants;

public class TenantService(TenantRepository tenantRepo)
{
    public async Task<TenantProfileResponse> UpdateProfileAsync(Guid tenantId, UpdateProfileRequest request)
    {
        var tenant = await tenantRepo.FindByIdAsync(tenantId)
            ?? throw new KeyNotFoundException("Tenant not found.");

        tenant.BusinessName = request.BusinessName;
        tenant.BusinessRegistrationNumber = request.BusinessRegistrationNumber;
        tenant.BusinessType = request.BusinessType;
        tenant.Industry = request.Industry;
        tenant.Country = request.Country;
        tenant.BusinessAddress = request.BusinessAddress;
        tenant.PhoneNumber = request.PhoneNumber;
        tenant.Website = request.Website;

        await tenantRepo.UpdateAsync(tenant);

        return MapToResponse(tenant);
    }

    public async Task<TenantProfileResponse> SetBusinessTypeAsync(Guid tenantId, string businessType)
    {
        var tenant = await tenantRepo.FindByIdAsync(tenantId)
            ?? throw new KeyNotFoundException("Tenant not found.");

        tenant.BusinessType = businessType;
        await tenantRepo.UpdateAsync(tenant);

        return MapToResponse(tenant);
    }

    public async Task<TenantProfileResponse> GetProfileAsync(Guid tenantId)
    {
        var tenant = await tenantRepo.FindByIdAsync(tenantId)
            ?? throw new KeyNotFoundException("Tenant not found.");

        return MapToResponse(tenant);
    }

    private static TenantProfileResponse MapToResponse(Tenant tenant) => new()
    {
        Id = tenant.Id,
        BusinessName = tenant.BusinessName,
        BusinessRegistrationNumber = tenant.BusinessRegistrationNumber,
        BusinessType = tenant.BusinessType,
        Industry = tenant.Industry,
        Country = tenant.Country,
        BusinessAddress = tenant.BusinessAddress,
        PhoneNumber = tenant.PhoneNumber,
        Website = tenant.Website,
        Email = tenant.Email,
        CreatedAt = tenant.CreatedAt
    };
}
