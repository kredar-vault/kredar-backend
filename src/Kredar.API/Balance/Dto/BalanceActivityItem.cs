namespace Kredar.API.Balance.Dto;

public class BalanceActivityItem
{
    public string Type { get; set; } = string.Empty; // "CREDIT" | "DEBIT"
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
