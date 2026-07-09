using Kredar.API.Common;
using Kredar.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.ApiLogs;

[ApiController]
[Authorize]
[Route("api/v1/api-logs")]
public class ApiLogsController(AppDbContext db) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? method,
        [FromQuery] string? path,
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = db.ApiRequestLogs.Where(l => l.TenantId == TenantId);

        if (!string.IsNullOrEmpty(method)) query = query.Where(l => l.Method == method.ToUpper());
        if (!string.IsNullOrEmpty(path)) query = query.Where(l => l.Path.Contains(path));
        if (statusCode.HasValue) query = query.Where(l => l.StatusCode == statusCode.Value);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(l => l.CreatedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id, l.Method, l.Path, l.QueryString, l.StatusCode,
                l.DurationMs, l.IpAddress, l.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Success(new { total, page, pageSize, items }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var log = await db.ApiRequestLogs
            .Where(l => l.Id == id && l.TenantId == TenantId)
            .FirstOrDefaultAsync(ct);

        if (log is null) return NotFound(ApiResponse<object>.Fail("Log not found."));
        return Ok(ApiResponse<object>.Success(log));
    }
}

[ApiController]
[Authorize]
[Route("api/v1/api-usage")]
public class ApiUsageController(AppDbContext db) : ControllerBase
{
    private Guid TenantId => Guid.Parse(User.FindFirst("tenantId")!.Value);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var logs = db.ApiRequestLogs.Where(l => l.TenantId == TenantId);

        var total = await logs.CountAsync(ct);
        var success = await logs.CountAsync(l => l.StatusCode >= 200 && l.StatusCode < 300, ct);
        var errors = await logs.CountAsync(l => l.StatusCode >= 400, ct);
        var avgMs = total > 0
            ? await logs.AverageAsync(l => (double?)l.DurationMs, ct) ?? 0
            : 0;

        var topEndpoints = await logs
            .GroupBy(l => new { l.Method, l.Path })
            .Select(g => new { g.Key.Method, g.Key.Path, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10)
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Success(new
        {
            total,
            successRate = total > 0 ? Math.Round((double)success / total * 100, 1) : 0,
            errorRate = total > 0 ? Math.Round((double)errors / total * 100, 1) : 0,
            avgResponseMs = Math.Round(avgMs, 0),
            topEndpoints,
        }));
    }
}
