namespace Kredar.API.DedicatedAccounts.Dto;

public class DedicatedAccountResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal? ExpectedAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentState { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
