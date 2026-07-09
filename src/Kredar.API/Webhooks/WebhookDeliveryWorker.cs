using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Webhooks;

public sealed class WebhookDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpFactory,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    private const int MaxAttempts = 8;
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DeliverDueAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "Webhook delivery poll failed."); }
            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task DeliverDueAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var due = await db.WebhookDeliveries
            .Where(d => (d.Status == WebhookDeliveryStatus.Pending || d.Status == WebhookDeliveryStatus.Failed)
                        && d.NextAttemptAt != null && d.NextAttemptAt <= now)
            .OrderBy(d => d.NextAttemptAt)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (due.Count == 0) return;

        var endpointIds = due.Select(d => d.EndpointId).Distinct().ToList();
        var byId = (await db.WebhookEndpoints.Where(e => endpointIds.Contains(e.Id)).ToListAsync(ct))
                   .ToDictionary(e => e.Id);

        var http = httpFactory.CreateClient("outbound-webhook");

        foreach (var delivery in due)
        {
            if (!byId.TryGetValue(delivery.EndpointId, out var endpoint) || !endpoint.Active)
            {
                delivery.RecordFailure("endpoint removed", null, MaxAttempts);
                continue;
            }

            try
            {
                var bytes = Encoding.UTF8.GetBytes(delivery.PayloadJson);
                var sig = Convert.ToHexString(
                    new HMACSHA256(Encoding.UTF8.GetBytes(endpoint.SigningSecret)).ComputeHash(bytes)).ToLowerInvariant();

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
                {
                    Content = new ByteArrayContent(bytes),
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Headers.TryAddWithoutValidation("x-kredar-signature", sig);
                request.Headers.TryAddWithoutValidation("x-kredar-event", delivery.EventType);
                request.Headers.TryAddWithoutValidation("x-kredar-delivery-id", delivery.Id.ToString());
                request.Headers.TryAddWithoutValidation("x-kredar-event-id", delivery.EventId);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                using var response = await http.SendAsync(request, cts.Token);

                if (response.IsSuccessStatusCode)
                    delivery.MarkDelivered((int)response.StatusCode);
                else
                    delivery.RecordFailure($"HTTP {(int)response.StatusCode}", (int)response.StatusCode, MaxAttempts);
            }
            catch (Exception ex)
            {
                delivery.RecordFailure(ex.Message, null, MaxAttempts);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
