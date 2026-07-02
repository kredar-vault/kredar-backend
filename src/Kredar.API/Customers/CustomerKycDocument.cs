namespace Kredar.API.Customers;

public enum KycDocumentType
{
    GovernmentId,
    ProfileImage,
    ProofOfAddress
}

public enum KycDocumentStatus
{
    Pending,
    Verified,
    Rejected
}

public class CustomerKycDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid TenantId { get; set; }
    public KycDocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public KycDocumentStatus Status { get; set; } = KycDocumentStatus.Pending;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
