namespace Kredar.API.Webhooks.Dto;

public class WebhookEndpointResponse
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? SigningSecret { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}
