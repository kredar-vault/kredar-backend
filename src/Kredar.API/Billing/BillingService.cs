using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Billing;

public class BillingService(AppDbContext db)
{
    public async Task<BillingSchedule> CreateAsync(
        Guid tenantId, string accountRef, BillingInterval interval,
        long amountKobo, int dueOffsetDays, string? description, string? reference, CancellationToken ct)
    {
        if (reference is not null)
        {
            var clash = await db.BillingSchedules.AnyAsync(s => s.TenantId == tenantId && s.Reference == reference, ct);
            if (clash) throw new InvalidOperationException("A billing schedule with that reference already exists.");
        }

        var existing = await db.BillingSchedules.AnyAsync(
            s => s.TenantId == tenantId && s.AccountRef == accountRef && s.Status == BillingScheduleStatus.Active, ct);
        if (existing) throw new InvalidOperationException("This account already has an active billing schedule.");

        var now = DateTime.UtcNow;
        var schedule = new BillingSchedule
        {
            TenantId = tenantId,
            AccountRef = accountRef,
            Interval = interval,
            NextAmountKobo = amountKobo,
            DueOffsetDays = dueOffsetDays,
            Description = description,
            Reference = reference,
            CreatedAtUtc = now
        };

        var period = OpenNextPeriod(schedule, now);
        schedule.Periods.Add(period);

        db.BillingSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task<List<BillingSchedule>> ListAsync(Guid tenantId, int take, CancellationToken ct) =>
        await db.BillingSchedules
            .Where(s => s.TenantId == tenantId)
            .Include(s => s.Periods)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public async Task<BillingSchedule> GetAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var schedule = await db.BillingSchedules
            .Include(s => s.Periods)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Billing schedule not found.");
        return schedule;
    }

    public async Task<List<BillingPeriod>> ListPeriodsAsync(Guid tenantId, Guid id, int take, CancellationToken ct)
    {
        var schedule = await GetAsync(tenantId, id, ct);
        return schedule.Periods.OrderByDescending(p => p.Sequence).Take(take).ToList();
    }

    public async Task<BillingSchedule> SetNextAmountAsync(Guid tenantId, Guid id, long amountKobo, CancellationToken ct)
    {
        var schedule = await GetAsync(tenantId, id, ct);
        schedule.NextAmountKobo = amountKobo;
        await db.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task<BillingSchedule> PauseAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var schedule = await GetAsync(tenantId, id, ct);
        if (schedule.Status != BillingScheduleStatus.Active)
            throw new InvalidOperationException("Only active schedules can be paused.");
        schedule.Status = BillingScheduleStatus.Paused;
        await db.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task<BillingSchedule> ResumeAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var schedule = await GetAsync(tenantId, id, ct);
        if (schedule.Status != BillingScheduleStatus.Paused)
            throw new InvalidOperationException("Only paused schedules can be resumed.");
        schedule.Status = BillingScheduleStatus.Active;
        await db.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task<BillingSchedule> CancelAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var schedule = await GetAsync(tenantId, id, ct);
        if (schedule.Status == BillingScheduleStatus.Cancelled)
            throw new InvalidOperationException("Schedule is already cancelled.");
        schedule.Status = BillingScheduleStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return schedule;
    }

    private static BillingPeriod OpenNextPeriod(BillingSchedule schedule, DateTime from)
    {
        var start = from;
        var end = schedule.Interval switch
        {
            BillingInterval.Weekly => start.AddDays(7),
            BillingInterval.Monthly => start.AddMonths(1),
            BillingInterval.Quarterly => start.AddMonths(3),
            BillingInterval.Yearly => start.AddYears(1),
            _ => start.AddMonths(1)
        };

        schedule.PeriodsGenerated++;
        schedule.CurrentPeriodEndUtc = end;

        return new BillingPeriod
        {
            ScheduleId = schedule.Id,
            Sequence = schedule.PeriodsGenerated,
            ExpectedAmountKobo = schedule.NextAmountKobo,
            PeriodStartUtc = start,
            PeriodEndUtc = end,
            DueDateUtc = end.AddDays(schedule.DueOffsetDays)
        };
    }
}
