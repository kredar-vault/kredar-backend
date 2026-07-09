namespace Kredar.API.Transfers;

public enum TransferStatus { Pending, Succeeded, Failed }

public class Transfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string MerchantTxRef { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RecipientAccountNumber { get; set; } = string.Empty;
    public string RecipientBankCode { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string? Narration { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
