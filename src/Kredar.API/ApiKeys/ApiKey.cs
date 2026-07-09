namespace Kredar.API.ApiKeys;

public enum ApiKeyMode { Test, Live }
public enum ApiKeyStatus { Active, Revoked }

public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public ApiKeyMode Mode { get; set; } = ApiKeyMode.Test;
    public ApiKeyStatus Status { get; set; } = ApiKeyStatus.Active;
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
