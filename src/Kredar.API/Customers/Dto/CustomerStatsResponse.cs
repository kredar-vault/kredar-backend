namespace Kredar.API.Customers.Dto;

public class CustomerStatsResponse
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int InactiveCustomers { get; set; }
}
