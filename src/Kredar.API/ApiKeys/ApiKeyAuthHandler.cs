using System.Security.Claims;
using System.Text.Encodings.Web;
using Kredar.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kredar.API.ApiKeys;

public class ApiKeyAuthOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<ApiKeyAuthOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authHeader["Bearer ".Length..].Trim();
        if (!token.StartsWith("sk_live_") && !token.StartsWith("sk_test_"))
            return AuthenticateResult.NoResult();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var key = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.ClientSecret == token && k.Status == ApiKeyStatus.Active);

        if (key == null)
            return AuthenticateResult.Fail("Invalid or revoked API key.");

        // Update last-used timestamp in background
        _ = Task.Run(async () =>
        {
            try
            {
                using var bg = scopeFactory.CreateScope();
                var bgDb = bg.ServiceProvider.GetRequiredService<AppDbContext>();
                await bgDb.ApiKeys
                    .Where(k => k.Id == key.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTime.UtcNow));
            }
            catch { }
        });

        var claims = new[]
        {
            new Claim("tenantId", key.TenantId.ToString()),
            new Claim("keyId", key.Id.ToString()),
            new Claim("keyMode", key.Mode.ToString()),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
