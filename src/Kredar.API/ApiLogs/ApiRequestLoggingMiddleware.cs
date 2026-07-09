using System.Diagnostics;
using Kredar.API.Data;

namespace Kredar.API.ApiLogs;

public class ApiRequestLoggingMiddleware(RequestDelegate next)
{
    // Skip health checks, swagger, webhooks from Nomba, and the log endpoint itself
    private static readonly string[] SkipPrefixes =
    [
        "/health", "/swagger", "/api/v1/nomba", "/api/v1/api-logs", "/api/v1/api-usage"
    ];

    public async Task InvokeAsync(HttpContext ctx, IServiceScopeFactory scopeFactory)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        if (SkipPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(ctx);
            return;
        }

        var sw = Stopwatch.StartNew();
        await next(ctx);
        sw.Stop();

        // Persist in background so we don't slow down the response
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Guid? tenantId = null;
                if (ctx.User.Identity?.IsAuthenticated == true)
                {
                    var claim = ctx.User.FindFirst("tenantId")?.Value;
                    if (Guid.TryParse(claim, out var tid)) tenantId = tid;
                }

                var log = new ApiRequestLog
                {
                    TenantId = tenantId,
                    Method = ctx.Request.Method,
                    Path = path,
                    QueryString = ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : null,
                    StatusCode = ctx.Response.StatusCode,
                    DurationMs = sw.ElapsedMilliseconds,
                    IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                };

                db.ApiRequestLogs.Add(log);
                await db.SaveChangesAsync();
            }
            catch { /* never let logging crash the app */ }
        });
    }
}
