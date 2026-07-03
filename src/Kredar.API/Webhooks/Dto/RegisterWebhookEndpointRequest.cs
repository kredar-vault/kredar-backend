using System.ComponentModel.DataAnnotations;

namespace Kredar.API.Webhooks.Dto;

public class RegisterWebhookEndpointRequest
{
    [Required, Url]
    public string Url { get; set; } = string.Empty;
}
