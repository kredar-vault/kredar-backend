using Kredar.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Billing;

public record CreateBillingScheduleRequest(
    string AccountRef,
    string Interval,
    long AmountKobo,
    int DueOffsetDays = 3,
    string? Description = null,
    string? Reference = null);

public record SetNextAmountRequest(long AmountKobo);

public record BillingScheduleResponse(
    Guid Id, string? Reference, string AccountRef, string Interval, string Status,
    long NextAmountKobo, int DueOffsetDays, int PeriodsGenerated, long CarryCreditKobo,
    DateTime? CurrentPeriodEndUtc, string? Description, DateTime CreatedAtUtc);

public record BillingPeriodResponse(
    Guid Id, int Sequence, string Status, long ExpectedAmountKobo, long AmountAttributedKobo,
    long OutstandingKobo, DateTime PeriodStartUtc, DateTime PeriodEndUtc, DateTime DueDateUtc, DateTime? PaidAtUtc);

[ApiController]
[Route("api/v1/billing")]
[Authorize]
[Tags("Billing")]
public class BillingController(BillingService billing) : ControllerBase
{
    [HttpPost("schedules")]
    public async Task<IActionResult> Create([FromBody] CreateBillingScheduleRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        if (!Enum.TryParse<BillingInterval>(request.Interval, ignoreCase: true, out var interval))
            return BadRequest(ApiResponse<string>.Fail("Interval must be one of: Weekly, Monthly, Quarterly, Yearly."));

        var schedule = await billing.CreateAsync(tenantId, request.AccountRef, interval, request.AmountKobo, request.DueOffsetDays, request.Description, request.Reference, ct);
        return StatusCode(201, ApiResponse<BillingScheduleResponse>.Success(ToResponse(schedule)));
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> List([FromQuery] int take = 50, CancellationToken ct = default)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var schedules = await billing.ListAsync(tenantId, take, ct);
        return Ok(ApiResponse<List<BillingScheduleResponse>>.Success(schedules.Select(ToResponse).ToList()));
    }

    [HttpGet("schedules/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var schedule = await billing.GetAsync(tenantId, id, ct);
        return Ok(ApiResponse<BillingScheduleResponse>.Success(ToResponse(schedule)));
    }

    [HttpGet("schedules/{id:guid}/periods")]
    public async Task<IActionResult> Periods(Guid id, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var periods = await billing.ListPeriodsAsync(tenantId, id, take, ct);
        return Ok(ApiResponse<List<BillingPeriodResponse>>.Success(periods.Select(ToPeriodResponse).ToList()));
    }

    [HttpPut("schedules/{id:guid}/next-amount")]
    public async Task<IActionResult> SetNextAmount(Guid id, [FromBody] SetNextAmountRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var schedule = await billing.SetNextAmountAsync(tenantId, id, request.AmountKobo, ct);
        return Ok(ApiResponse<BillingScheduleResponse>.Success(ToResponse(schedule)));
    }

    [HttpPost("schedules/{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var schedule = await billing.PauseAsync(tenantId, id, ct);
        return Ok(ApiResponse<BillingScheduleResponse>.Success(ToResponse(schedule)));
    }

    [HttpPost("schedules/{id:guid}/resume")]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var schedule = await billing.ResumeAsync(tenantId, id, ct);
        return Ok(ApiResponse<BillingScheduleResponse>.Success(ToResponse(schedule)));
    }

    [HttpPost("schedules/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var schedule = await billing.CancelAsync(tenantId, id, ct);
        return Ok(ApiResponse<BillingScheduleResponse>.Success(ToResponse(schedule)));
    }

    private static BillingScheduleResponse ToResponse(BillingSchedule s) => new(
        s.Id, s.Reference, s.AccountRef, s.Interval.ToString(), s.Status.ToString(),
        s.NextAmountKobo, s.DueOffsetDays, s.PeriodsGenerated, s.CarryCreditKobo,
        s.CurrentPeriodEndUtc, s.Description, s.CreatedAtUtc);

    private static BillingPeriodResponse ToPeriodResponse(BillingPeriod p) => new(
        p.Id, p.Sequence, p.Status.ToString(), p.ExpectedAmountKobo, p.AmountAttributedKobo,
        p.OutstandingKobo, p.PeriodStartUtc, p.PeriodEndUtc, p.DueDateUtc, p.PaidAtUtc);
}
