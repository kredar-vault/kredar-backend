using Kredar.API.Auth.Dto;
using Kredar.API.Tenants;

namespace Kredar.API.Auth;

public class AuthService(TenantRepository tenantRepo, JwtService jwtService)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await tenantRepo.FindByEmailAsync(request.Email);
        if (existing != null)
            throw new Exception("Email already registered.");

        var tenant = new Tenant
        {
            BusinessName = request.BusinessName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsVerified = true
        };

        await tenantRepo.AddAsync(tenant);

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(tenant.Id, tenant.Email),
            BusinessName = tenant.BusinessName,
            Email = tenant.Email
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var tenant = await tenantRepo.FindByEmailAsync(request.Email)
            ?? throw new Exception("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, tenant.PasswordHash))
            throw new Exception("Invalid email or password.");

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(tenant.Id, tenant.Email),
            BusinessName = tenant.BusinessName,
            Email = tenant.Email
        };
    }
}
