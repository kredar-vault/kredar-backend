namespace Kredar.API.Revenue.Dto;

public class RevenueResponse
{
    public bool Enabled { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal AvailableRevenue { get; set; } // TotalRevenue minus already-withdrawn
}

public class RevenueWithdrawRequest
{
    public decimal Amount { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Narration { get; set; } = "Revenue Withdrawal";
}
