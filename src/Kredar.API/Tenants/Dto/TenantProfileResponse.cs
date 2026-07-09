namespace Kredar.API.Tenants.Dto;

public class TenantProfileResponse
{
    public Guid Id { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? BusinessAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
