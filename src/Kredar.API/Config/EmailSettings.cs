namespace Kredar.API.Config;

/// <summary>
/// Transactional email via Resend (https://resend.com), mirroring the Xental setup.
/// Bound from the "Resend" configuration section; the API key is injected from the
/// environment (Resend__ApiKey) in deployed environments.
/// </summary>
public class ResendSettings
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Kredar";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(FromEmail);
}
