using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kredar.API.Config;
using Microsoft.Extensions.Options;

namespace Kredar.API.Nomba;

public sealed class NombaSignatureVerifier(IOptions<NombaSettings> options)
{
    private readonly NombaSettings _settings = options.Value;

    public bool Verify(byte[] rawBody, string? signatureHeader, string? timestampHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        string hashingPayload;
        try
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : default;
            var merchant = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("merchant", out var m) ? m : default;
            var txn = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("transaction", out var t) ? t : default;

            hashingPayload = string.Join(":",
                Str(root, "event_type"),
                Str(root, "requestId"),
                Str(merchant, "userId"),
                Str(merchant, "walletId"),
                Str(txn, "transactionId"),
                Str(txn, "type"),
                Str(txn, "time"),
                Str(txn, "responseCode"),
                timestampHeader ?? string.Empty);
        }
        catch (JsonException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(hashingPayload)));

        // Base64 is case-sensitive — compare without normalising case
        var a = Encoding.UTF8.GetBytes(computed);
        var b = Encoding.UTF8.GetBytes(signatureHeader.Trim());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Str(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? string.Empty : string.Empty;
}
