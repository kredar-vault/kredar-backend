namespace Kredar.API.Webhooks;

public enum WebhookDeliveryStatus { Pending = 1, Delivered = 2, Failed = 3, DeadLetter = 4 }

public class WebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid EndpointId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    public int Attempts { get; set; } = 0;
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? LastError { get; set; }
    public int? LastStatusCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void MarkDelivered(int statusCode)
    {
        Attempts++;
        Status = WebhookDeliveryStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        LastStatusCode = statusCode;
        NextAttemptAt = null;
        LastError = null;
    }

    public void RecordFailure(string error, int? statusCode, int maxAttempts)
    {
        Attempts++;
        LastError = error.Length > 500 ? error[..500] : error;
        LastStatusCode = statusCode;
        if (Attempts >= maxAttempts)
        {
            Status = WebhookDeliveryStatus.DeadLetter;
            NextAttemptAt = null;
        }
        else
        {
            Status = WebhookDeliveryStatus.Failed;
            var delay = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, Attempts - 1)));
            NextAttemptAt = DateTime.UtcNow + delay;
        }
    }

    public void Replay()
    {
        Status = WebhookDeliveryStatus.Pending;
        Attempts = 0;
        NextAttemptAt = DateTime.UtcNow;
        LastError = null;
    }
}
