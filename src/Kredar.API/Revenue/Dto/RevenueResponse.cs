namespace Kredar.API.Revenue.Dto;

public class RevenueResponse
{
    public bool Enabled { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
}
