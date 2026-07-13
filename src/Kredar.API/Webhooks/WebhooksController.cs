using System.ComponentModel.DataAnnotations;
using Kredar.API.Common;
using Kredar.API.Config;
using Kredar.API.Webhooks.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kredar.API.Webhooks;

public class SimulateDepositRequest
{
    [Required] public string NombaReference { get; set; } = string.Empty;
    [Required] public string AccountNumber { get; set; } = string.Empty;
    [Required] public decimal AmountNaira { get; set; }
    public string? SenderName { get; set; }
}

[ApiController]
public class WebhooksController(
    NombaWebhookService nombaWebhookService,
    WebhookEndpointService endpointService,
    WebhookDeliveryRepository deliveryRepo,
    IOptions<NombaSettings> nombaOptions) : ControllerBase
{
    [HttpPost("webhooks/nomba")]
    [HttpPost("api/v1/webhooks/nomba")]
    [AllowAnonymous]
    public async Task<IActionResult> NombaWebhook()
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var rawBody = ms.ToArray();

        var settings = nombaOptions.Value;
        var signature = Request.Headers[settings.WebhookSignatureHeader].FirstOrDefault();
        var timestamp = Request.Headers[settings.TimestampHeader].FirstOrDefault();

        var processed = await nombaWebhookService.ProcessAsync(rawBody, signature, timestamp);
        if (!processed) return Unauthorized();
        return Ok();
    }

    // Manually reconcile a missed deposit — bypasses HMAC, scoped to the calling tenant's DVAs.
    [HttpPost("api/v1/webhooks/simulate")]
    [Authorize]
    public async Task<IActionResult> SimulateDeposit([FromBody] SimulateDepositRequest req)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var parsed = new NombaParsedEvent(
            req.NombaReference, req.AccountNumber, (long)(req.AmountNaira * 100),
            0, req.SenderName ?? "Manual reconciliation", "virtual_account.funded", false);

        var (status, reference) = await nombaWebhookService.ReconcileForTenantAsync(tenantId, parsed);
        return Ok(ApiResponse<object>.Success(new { status, reference }, "Transaction reconciled."));
    }

    [HttpPost("api/v1/webhook-endpoints")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterWebhookEndpointRequest req)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await endpointService.RegisterAsync(tenantId, req);
        return Ok(ApiResponse<WebhookEndpointResponse>.Success(result, "Webhook endpoint registered. Save your signing secret — it won't be shown again."));
    }

    [HttpGet("api/v1/webhook-endpoints")]
    [Authorize]
    public async Task<IActionResult> List()
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var result = await endpointService.ListAsync(tenantId);
        return Ok(ApiResponse<List<WebhookEndpointResponse>>.Success(result));
    }

    [HttpDelete("api/v1/webhook-endpoints/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        await endpointService.DeleteAsync(tenantId, id);
        return Ok(ApiResponse<object>.Success(new { }, "Webhook endpoint deleted."));
    }

    [HttpGet("api/v1/webhook-deliveries")]
    [Authorize]
    public async Task<IActionResult> ListDeliveries([FromQuery] WebhookDeliveryStatus? status)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var deliveries = await deliveryRepo.GetByTenantAsync(tenantId, status);
        var result = deliveries.Select(d => new WebhookDeliveryResponse
        {
            Id = d.Id,
            EndpointId = d.EndpointId,
            EventId = d.EventId,
            EventType = d.EventType,
            Status = d.Status.ToString(),
            Attempts = d.Attempts,
            NextAttemptAt = d.NextAttemptAt,
            DeliveredAt = d.DeliveredAt,
            LastError = d.LastError,
            LastStatusCode = d.LastStatusCode,
            CreatedAt = d.CreatedAt,
        }).ToList();
        return Ok(ApiResponse<List<WebhookDeliveryResponse>>.Success(result));
    }

    [HttpPost("api/v1/webhook-deliveries/{id:guid}/replay")]
    [Authorize]
    public async Task<IActionResult> Replay(Guid id)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);
        var delivery = await deliveryRepo.FindByIdAsync(tenantId, id)
            ?? throw new KeyNotFoundException("Delivery not found.");
        delivery.Replay();
        await deliveryRepo.UpdateAsync(delivery);
        return Ok(ApiResponse<object>.Success(new { }, "Delivery requeued."));
    }
}
