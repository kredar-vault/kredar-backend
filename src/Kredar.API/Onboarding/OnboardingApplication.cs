using Kredar.API.Tenants;

namespace Kredar.API.Onboarding;

public enum OnboardingTier { Sandbox, Live }
public enum OnboardingStatus { NotStarted, InProgress, UnderReview, Approved, Rejected, MoreInfoRequired }

public class OnboardingApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public OnboardingTier Tier { get; set; } = OnboardingTier.Sandbox;
    public OnboardingStatus Status { get; set; } = OnboardingStatus.NotStarted;

    // Business KYB
    public string? LegalName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? ContactPhone { get; set; }
    public string? Website { get; set; }

    // Settlement account
    public string? SettlementBankName { get; set; }
    public string? SettlementBankCode { get; set; }
    public string? SettlementAccountName { get; set; }
    public string? SettlementAccountNumber { get; set; }

    public string? DecisionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DecidedAt { get; set; }

    public Tenant? Tenant { get; set; }
}
