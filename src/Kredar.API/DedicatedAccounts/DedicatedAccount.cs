namespace Kredar.API.DedicatedAccounts;

public enum DedicatedAccountStatus { Active, Closed }
public enum PaymentState { Unpaid, PartiallyPaid, FullyPaid, Overpaid }

public class DedicatedAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = "Nomba";
    public string AccountName { get; set; } = string.Empty;
    public string? ProviderAccountId { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal AmountPaid { get; set; } = 0;
    public Guid? SubMerchantId { get; set; }
    public DedicatedAccountStatus Status { get; set; } = DedicatedAccountStatus.Active;
    public PaymentState PaymentState { get; set; } = PaymentState.Unpaid;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
