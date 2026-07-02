namespace Kredar.API.Transactions.Dto;

public class CustomerTransactionStatsResponse
{
    public decimal TotalPaymentsToday { get; set; }
    public int PendingTransactions { get; set; }
    public int Exceptions { get; set; }
}
