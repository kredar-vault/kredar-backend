namespace Kredar.API.Transactions.Dto;

public class TransactionResponse
{
    // Transaction Summary
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Account details
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? DedicatedAccountNumber { get; set; }
    public string? Narration { get; set; }

    // Reconciliation details
    public decimal? ExpectedAmount { get; set; }
    public decimal? AmountReceived { get; set; }
    public decimal? Difference { get; set; }
}
