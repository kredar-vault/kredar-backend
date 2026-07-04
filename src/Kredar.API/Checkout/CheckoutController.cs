using System.Text.Json;
using Kredar.API.Common;
using Kredar.API.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredar.API.Checkout;

public class CreateCheckoutSessionRequest
{
    public string AccountReference { get; set; } = string.Empty;
    public int? TtlSeconds { get; set; }
}

public record CheckoutSessionResponse(string Token, string SnapshotUrl, string StreamUrl, DateTime ExpiresAt, CheckoutSnapshot Snapshot);

[ApiController]
public class CheckoutController(CheckoutService checkout, CheckoutEventBus eventBus) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [HttpPost("/api/v1/checkout/sessions")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCheckoutSessionRequest request)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var (session, account) = await checkout.CreateSessionAsync(tenantId, request.AccountReference, request.TtlSeconds);
        var response = new CheckoutSessionResponse(
            session.Token,
            $"/api/v1/checkout/{session.Token}",
            $"/api/v1/checkout/{session.Token}/stream",
            session.ExpiresAt,
            CheckoutService.Snapshot(account));
        return Ok(ApiResponse<CheckoutSessionResponse>.Success(response));
    }

    [HttpGet("/api/v1/checkout/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> Snapshot(string token)
    {
        var resolved = await checkout.ResolveAsync(token);
        if (resolved == null) return NotFound(ApiResponse<object>.Fail("Checkout session not found or expired."));
        return Ok(ApiResponse<CheckoutSnapshot>.Success(CheckoutService.Snapshot(resolved.Value.Account)));
    }

    [HttpGet("/api/v1/checkout/{token}/stream")]
    [AllowAnonymous]
    public async Task Stream(string token, CancellationToken ct)
    {
        var resolved = await checkout.ResolveAsync(token);
        if (resolved == null)
        {
            Response.StatusCode = 404;
            return;
        }

        var account = resolved.Value.Account;
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await WriteSseAsync(CheckoutService.Snapshot(account), ct);

        try
        {
            await foreach (var snapshot in eventBus.SubscribeAsync(account.Id, ct))
                await WriteSseAsync(snapshot, ct);
        }
        catch (OperationCanceledException) { }
    }

    private async Task WriteSseAsync(CheckoutSnapshot snapshot, CancellationToken ct)
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(snapshot, Json)}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
