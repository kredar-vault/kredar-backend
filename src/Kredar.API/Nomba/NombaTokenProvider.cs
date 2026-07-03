using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Kredar.API.Config;
using Microsoft.Extensions.Options;

namespace Kredar.API.Nomba;

public sealed class NombaTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<NombaSettings> options,
    ILogger<NombaTokenProvider> logger)
{
    private readonly NombaSettings _settings = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _refreshAfter;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _refreshAfter)
            return _token;

        await _gate.WaitAsync(ct);
        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _refreshAfter)
                return _token;

            var http = httpClientFactory.CreateClient("nomba");
            using var request = new HttpRequestMessage(HttpMethod.Post, "auth/token/issue")
            {
                Content = JsonContent.Create(new
                {
                    grant_type = "client_credentials",
                    client_id = _settings.ClientId,
                    client_secret = _settings.ClientSecret,
                }),
            };
            if (!string.IsNullOrWhiteSpace(_settings.AccountId))
                request.Headers.TryAddWithoutValidation("accountId", _settings.AccountId);

            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            var accessToken = payload?.Data?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("Nomba token response did not contain an access token.");

            _token = accessToken;
            _refreshAfter = DateTimeOffset.UtcNow.AddSeconds(_settings.TokenRefreshSeconds);
            logger.LogInformation("Refreshed Nomba access token; next refresh after {RefreshAfter:o}.", _refreshAfter);
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record TokenResponse([property: JsonPropertyName("data")] TokenData? Data);
    private sealed record TokenData([property: JsonPropertyName("access_token")] string? AccessToken);
}
