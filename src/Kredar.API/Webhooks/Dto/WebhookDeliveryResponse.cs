namespace Kredar.API.Webhooks.Dto;

public class WebhookDeliveryResponse
{
    public Guid Id { get; set; }
    public Guid EndpointId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? LastError { get; set; }
    public int? LastStatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
