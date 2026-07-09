namespace Kredar.API.Transfers.Dto;

public class TransferResponse
{
    public Guid Id { get; set; }
    public string MerchantTxRef { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RecipientAccountNumber { get; set; } = string.Empty;
    public string RecipientBankCode { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string? Narration { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BankLookupResponse
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
}
