namespace Kredar.API.Billing;

public enum BillingInterval { Weekly, Monthly, Quarterly, Yearly }
public enum BillingScheduleStatus { Active, Paused, Cancelled }
public enum BillingPeriodStatus { Open, Paid, Overdue }

public class BillingSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string AccountRef { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public BillingInterval Interval { get; set; }
    public BillingScheduleStatus Status { get; set; } = BillingScheduleStatus.Active;
    public long NextAmountKobo { get; set; }
    public int DueOffsetDays { get; set; } = 3;
    public int PeriodsGenerated { get; set; } = 0;
    public long CarryCreditKobo { get; set; } = 0;
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<BillingPeriod> Periods { get; set; } = [];
}

public class BillingPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScheduleId { get; set; }
    public int Sequence { get; set; }
    public BillingPeriodStatus Status { get; set; } = BillingPeriodStatus.Open;
    public long ExpectedAmountKobo { get; set; }
    public long AmountAttributedKobo { get; set; } = 0;
    public long OutstandingKobo => Math.Max(0, ExpectedAmountKobo - AmountAttributedKobo);
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public DateTime DueDateUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public BillingSchedule? Schedule { get; set; }
}
