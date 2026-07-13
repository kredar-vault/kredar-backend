using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kredar.API.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kredar.API.Nomba;

public sealed class NombaSignatureVerifier(IOptions<NombaSettings> options, ILogger<NombaSignatureVerifier> logger)
{
    private readonly NombaSettings _settings = options.Value;

    public bool Verify(byte[] rawBody, string? signatureHeader, string? timestampHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            logger.LogWarning("NombaVerifier: early exit — secret empty={SecretEmpty} signatureHeader empty={SigEmpty}",
                string.IsNullOrWhiteSpace(_settings.WebhookSecret),
                string.IsNullOrWhiteSpace(signatureHeader));
            return false;
        }

        string hashingPayload;
        try
        {
            var bodyStr = Encoding.UTF8.GetString(rawBody);
            logger.LogWarning("NombaVerifier: raw body = {Body}", bodyStr);
            logger.LogWarning("NombaVerifier: nomba-signature = {Sig}", signatureHeader);
            logger.LogWarning("NombaVerifier: nomba-timestamp = {Ts}", timestampHeader ?? "(null)");

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

            logger.LogWarning("NombaVerifier: hashingPayload = {Payload}", hashingPayload);
        }
        catch (JsonException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(hashingPayload)));

        logger.LogWarning("NombaVerifier: computed={Computed} received={Received}", computed, signatureHeader.Trim());

        // Base64 is case-sensitive — compare without normalising case
        var a = Encoding.UTF8.GetBytes(computed);
        var b = Encoding.UTF8.GetBytes(signatureHeader.Trim());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string Str(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? string.Empty : string.Empty;
}
