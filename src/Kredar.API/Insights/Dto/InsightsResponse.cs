namespace Kredar.API.Insights.Dto;

public class BalanceResponse
{
    public decimal AvailableBalance { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalTransferred { get; set; }
    public string Currency { get; set; } = "NGN";
}

public class InsightsResponse
{
    public decimal AvailableBalance { get; set; }
    public decimal TotalFees { get; set; }
    public int DedicatedAccounts { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalExpected { get; set; }
    public decimal OutstandingDeficit { get; set; }
    public double CollectionRatePct { get; set; }
    public int Reconciled { get; set; }
    public int Underpaid { get; set; }
    public int Overpaid { get; set; }
    public int Reversed { get; set; }
    public int FullyPaidAccounts { get; set; }
    public int PartiallyPaidAccounts { get; set; }
    public int TotalTransfers { get; set; }
    public decimal TotalTransferred { get; set; }
}
