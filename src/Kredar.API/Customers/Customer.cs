namespace Kredar.API.Customers;

public enum KycStatus
{
    Pending,
    InReview,
    Verified
}

public enum CustomerStatus
{
    Active,
    Inactive,
    Pending,
    Restricted
}

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? DedicatedAccountNumber { get; set; }
    public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
