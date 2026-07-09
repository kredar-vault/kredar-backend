using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kredar.API.Config;
using Microsoft.Extensions.Options;

namespace Kredar.API.Nomba;

public sealed record ProvisionedAccount(string AccountNumber, string BankName, string AccountName, string? ProviderId);
public sealed record NombaTransactionRecord(string Reference, string AccountNumber, long AmountKobo, long FeeKobo, string? SenderName);
public sealed record BankLookupResult(string AccountName, string AccountNumber, string BankCode);
public sealed record TransferResult(bool Success, string? Reference, string? Error);

public sealed class NombaClient(
    IHttpClientFactory httpClientFactory,
    NombaTokenProvider tokenProvider,
    IOptions<NombaSettings> options)
{
    private readonly NombaSettings _settings = options.Value;

    public async Task<ProvisionedAccount> CreateDedicatedAccountAsync(
        string accountRef, string accountName, string? email, string? phone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SubAccountId))
            throw new InvalidOperationException("NombaSettings.SubAccountId is not configured.");

        var http = httpClientFactory.CreateClient("nomba");
        var token = await tokenProvider.GetAccessTokenAsync(ct);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"accounts/virtual/{_settings.SubAccountId}")
        {
            Content = JsonContent.Create(new { accountRef, accountName, email, phoneNumber = phone }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrWhiteSpace(_settings.AccountId))
            request.Headers.TryAddWithoutValidation("accountId", _settings.AccountId);

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nomba account creation failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;

        var accountNumber = Str(data, "bankAccountNumber") ?? Str(data, "accountNumber") ?? Str(data, "accountNo")
            ?? throw new InvalidOperationException($"Nomba response missing account number: {body}");

        return new ProvisionedAccount(
            accountNumber,
            Str(data, "bankName") ?? "Nomba",
            Str(data, "bankAccountName") ?? Str(data, "accountName") ?? accountName,
            Str(data, "id") ?? Str(data, "accountId") ?? Str(data, "orderReference"));
    }

    public async Task<BankLookupResult> LookupBankAccountAsync(string accountNumber, string bankCode, CancellationToken ct = default)
    {
        using var doc = await PostAsync("transfers/bank/lookup", new { accountNumber, bankCode }, ct);
        var data = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;
        var name = Str(data, "accountName") ?? Str(data, "bankAccountName") ?? Str(data, "name")
            ?? throw new InvalidOperationException("Nomba lookup did not return an account name.");
        return new BankLookupResult(name, accountNumber, bankCode);
    }

    public async Task<TransferResult> InitiateTransferAsync(
        string merchantTxRef, decimal amountNaira, string accountNumber, string bankCode,
        string? accountName, string? narration, CancellationToken ct = default)
    {
        try
        {
            using var doc = await PostAsync("transfers/bank", new
            {
                merchantTxRef,
                amount = amountNaira,
                accountNumber,
                accountName = accountName ?? "",
                bankCode,
                narration = narration ?? "Kredar payout",
                senderName = "Kredar",
            }, ct);
            var data = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;
            var reference = Str(data, "id") ?? Str(data, "transactionId") ?? Str(data, "reference") ?? merchantTxRef;
            var status = Str(data, "status") ?? "PENDING";
            var success = status.Contains("success", StringComparison.OrdinalIgnoreCase)
                          || status.Contains("pending", StringComparison.OrdinalIgnoreCase);
            return new TransferResult(success, reference, success ? null : status);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, null, ex.Message);
        }
    }

    public async Task<List<NombaTransactionRecord>> GetRecentTransactionsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var http = httpClientFactory.CreateClient("nomba");
        var token = await tokenProvider.GetAccessTokenAsync(ct);
        var dateFrom = from.ToString("yyyy-MM-ddTHH:mm:ss");
        var dateTo = to.ToString("yyyy-MM-ddTHH:mm:ss");
        // Nomba scopes virtual-account transactions under the sub-account
        var subAccountId = _settings.SubAccountId;
        var path = !string.IsNullOrWhiteSpace(subAccountId)
            ? $"transactions/accounts/{subAccountId}?dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}"
            : $"transactions?dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrWhiteSpace(_settings.AccountId))
            request.Headers.TryAddWithoutValidation("accountId", _settings.AccountId);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return [];

        var raw = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var results = new List<NombaTransactionRecord>();
        JsonElement dataEl = root.TryGetProperty("data", out var d) ? d : root;

        JsonElement items = default;
        if (dataEl.ValueKind == JsonValueKind.Array) items = dataEl;
        else if (dataEl.TryGetProperty("results", out var r)) items = r;
        else if (dataEl.TryGetProperty("transactions", out var t)) items = t;
        else if (dataEl.TryGetProperty("items", out var i)) items = i;
        else if (dataEl.TryGetProperty("content", out var c)) items = c;

        if (items.ValueKind != JsonValueKind.Array) return results;

        foreach (var txn in items.EnumerateArray())
        {
            var reference = Str(txn, "transactionId") ?? Str(txn, "transaction_id")
                ?? Str(txn, "reference") ?? Str(txn, "id");
            // DVA webhooks use aliasAccountNumber for the NUBAN
            var accountNumber = Str(txn, "aliasAccountNumber") ?? Str(txn, "alias_account_number")
                ?? Str(txn, "bankAccountNumber") ?? Str(txn, "accountNumber")
                ?? Str(txn, "destinationAccountNumber");
            if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(accountNumber)) continue;

            // Nomba sends amounts in naira — multiply by 100 to get kobo
            var amountKobo = NairaToKobo(txn, "transactionAmount") ?? NairaToKobo(txn, "amount") ?? 0;
            var feeKobo = NairaToKobo(txn, "fee") ?? NairaToKobo(txn, "transactionFee") ?? 0;

            results.Add(new NombaTransactionRecord(reference, accountNumber, amountKobo, feeKobo,
                Str(txn, "senderName") ?? Str(txn, "originatorName") ?? Str(txn, "bankAccountName")));
        }
        return results;
    }

    private async Task<JsonDocument> PostAsync(string path, object body, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient("nomba");
        var token = await tokenProvider.GetAccessTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrWhiteSpace(_settings.AccountId))
            request.Headers.TryAddWithoutValidation("accountId", _settings.AccountId);
        using var response = await http.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Nomba {path} failed ({(int)response.StatusCode}): {raw}");
        return JsonDocument.Parse(raw);
    }

    private static string? Str(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static long? NairaToKobo(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(name, out var a)) return null;
        if (a.ValueKind == JsonValueKind.Number && a.TryGetDecimal(out var naira))
            return (long)decimal.Round(naira * 100m, 0, MidpointRounding.ToEven);
        if (a.ValueKind == JsonValueKind.String &&
            decimal.TryParse(a.GetString(), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out naira))
            return (long)decimal.Round(naira * 100m, 0, MidpointRounding.ToEven);
        return null;
    }
}
