using Kredar.API.Tenants;

namespace Kredar.API.Onboarding;

public enum OnboardingTier { Sandbox, Live }
public enum OnboardingStatus { NotStarted, InProgress, UnderReview, Approved, Rejected, MoreInfoRequired }
public enum KycStatus { NotStarted, UnderReview, Approved, Rejected }
public enum GovIdType { Bvn, Nin }
public enum KycDocumentType { CertificateOfIncorporation, ProofOfAddress }

public class OnboardingApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public OnboardingTier Tier { get; set; } = OnboardingTier.Sandbox;
    public OnboardingStatus Status { get; set; } = OnboardingStatus.NotStarted;

    // Developer KYC
    public KycStatus DeveloperKycStatus { get; set; } = KycStatus.NotStarted;
    public string? DevFullName { get; set; }
    public string? DevDateOfBirth { get; set; }
    public string? DevCountry { get; set; }
    public string? DevAddress { get; set; }
    public GovIdType? DevGovIdType { get; set; }
    public string? DevGovIdNumber { get; set; }
    public string? DevBankName { get; set; }
    public string? DevBankCode { get; set; }
    public string? DevAccountName { get; set; }
    public string? DevAccountNumber { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? ProjectDescription { get; set; }

    // Business KYB
    public KycStatus BusinessKybStatus { get; set; } = KycStatus.NotStarted;
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
    public List<OnboardingDocument> Documents { get; set; } = [];
}

public class OnboardingDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OnboardingApplicationId { get; set; }
    public KycDocumentType DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
