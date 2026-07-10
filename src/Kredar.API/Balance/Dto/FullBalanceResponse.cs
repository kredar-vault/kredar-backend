namespace Kredar.API.Balance.Dto;

public class FullBalanceResponse
{
    public decimal AvailableBalance { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal OnHoldBalance { get; set; }
    public decimal IncomingToday { get; set; }
    public decimal SettledToday { get; set; }
    public string Currency { get; set; } = "NGN";
    public bool CanWithdraw { get; set; }
}
