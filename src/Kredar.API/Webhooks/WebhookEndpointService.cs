using Kredar.API.Webhooks.Dto;

namespace Kredar.API.Webhooks;

public class WebhookEndpointService(WebhookEndpointRepository repo)
{
    public async Task<WebhookEndpointResponse> RegisterAsync(Guid tenantId, RegisterWebhookEndpointRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
            throw new ArgumentException("Webhook URL is required.");

        var secret = $"whsec_{Guid.NewGuid():N}{Guid.NewGuid():N}";
        var endpoint = new WebhookEndpoint
        {
            TenantId = tenantId,
            Url = req.Url.Trim(),
            SigningSecret = secret,
        };
        await repo.AddAsync(endpoint);

        return new WebhookEndpointResponse
        {
            Id = endpoint.Id,
            Url = endpoint.Url,
            SigningSecret = secret,
            Active = endpoint.Active,
            CreatedAt = endpoint.CreatedAt,
        };
    }

    public async Task<List<WebhookEndpointResponse>> ListAsync(Guid tenantId)
    {
        var endpoints = await repo.GetAllAsync(tenantId);
        return endpoints.Select(e => new WebhookEndpointResponse
        {
            Id = e.Id,
            Url = e.Url,
            Active = e.Active,
            CreatedAt = e.CreatedAt,
        }).ToList();
    }

    public async Task DeleteAsync(Guid tenantId, Guid id)
    {
        var endpoint = await repo.FindByIdAsync(tenantId, id)
            ?? throw new KeyNotFoundException("Webhook endpoint not found.");
        endpoint.Active = false;
        await repo.UpdateAsync(endpoint);
    }
}
