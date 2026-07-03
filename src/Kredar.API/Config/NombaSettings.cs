namespace Kredar.API.Config;

public class NombaSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string SubAccountId { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookSignatureHeader { get; set; } = "nomba-signature";
    public string TimestampHeader { get; set; } = "nomba-timestamp";
    public int TokenRefreshSeconds { get; set; } = 3300;
}
