namespace Kredar.API.Tenants;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? BusinessName { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? BusinessAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }
    public string? LoginOtp { get; set; }
    public DateTime? LoginOtpExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
