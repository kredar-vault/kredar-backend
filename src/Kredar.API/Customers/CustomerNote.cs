namespace Kredar.API.Customers;

public class CustomerNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CreatedByEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
