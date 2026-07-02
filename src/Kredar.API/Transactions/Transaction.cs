namespace Kredar.API.Transactions;

public enum TransactionStatus
{
    Pending,
    Reconciled,
    Overpaid,
    Underpaid,
    Failed,
    Reversed
}

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; } = 0;
    public string Currency { get; set; } = "NGN";
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public string? DedicatedAccountNumber { get; set; }
    public string? Narration { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? AmountReceived { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
