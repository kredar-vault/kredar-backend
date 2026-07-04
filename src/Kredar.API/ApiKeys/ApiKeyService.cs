using Kredar.API.Common;
using Kredar.API.Data;
using Kredar.API.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.ApiKeys;

public record CreatedApiKey(Guid Id, string ClientId, string ClientSecret, string Mode, string Label, DateTime CreatedAt);

public class ApiKeyService(ApiKeyRepository repo, AppDbContext db)
{
    public async Task<CreatedApiKey> CreateAsync(Guid tenantId, string label, ApiKeyMode mode)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new Exception("Key label is required.");

        if (mode == ApiKeyMode.Live)
        {
            var onboarding = await db.OnboardingApplications
                .FirstOrDefaultAsync(a => a.TenantId == tenantId);
            if (onboarding?.Tier != OnboardingTier.Live)
                throw new UnauthorizedAccessException("Live keys require KYB approval. Submit your onboarding application and wait for admin review.");
        }

        var prefix = mode == ApiKeyMode.Live ? "krd_live" : "krd_test";
        var secretPrefix = mode == ApiKeyMode.Live ? "sk_live" : "sk_test";
        var clientId = prefix + "_" + Guid.NewGuid().ToString("N")[..12];
        var secret = secretPrefix + "_" + Guid.NewGuid().ToString("N");

        var key = new ApiKey
        {
            TenantId = tenantId,
            Label = label.Trim(),
            ClientId = clientId,
            SecretHash = BCrypt.Net.BCrypt.HashPassword(secret, workFactor: 6),
            Mode = mode
        };

        await repo.AddAsync(key);
        return new CreatedApiKey(key.Id, clientId, secret, mode.ToString(), key.Label, key.CreatedAt);
    }

    public async Task<List<ApiKey>> ListAsync(Guid tenantId) =>
        await repo.GetByTenantAsync(tenantId);

    public async Task RevokeAsync(Guid id, Guid tenantId)
    {
        var key = await repo.FindByIdAsync(id, tenantId)
            ?? throw new Exception("API key not found.");
        key.Status = ApiKeyStatus.Revoked;
        await repo.UpdateAsync(key);
    }

    public async Task<CreatedApiKey> RotateAsync(Guid id, Guid tenantId)
    {
        var key = await repo.FindByIdAsync(id, tenantId)
            ?? throw new Exception("API key not found.");
        key.Status = ApiKeyStatus.Revoked;
        await repo.UpdateAsync(key);
        return await CreateAsync(tenantId, key.Label, key.Mode);
    }
}
