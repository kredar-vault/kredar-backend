using System.Security.Cryptography;
using Kredar.API.Common;
using Kredar.API.Data;
using Kredar.API.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Sandbox;

public class SimulateDepositRequest
{
    public string AccountReference { get; set; } = string.Empty;
    public decimal AmountNaira { get; set; }
    public string? SenderName { get; set; }
    public bool Reversal { get; set; } = false;
}

public record SimulatedDepositResponse(string Status, string KredarReference, string Reconciliation, string PaymentState);

[ApiController]
[Route("api/v1/sandbox")]
[Authorize]
public class SandboxController(AppDbContext db, NombaWebhookService webhook) : ControllerBase
{
    [HttpPost("simulate/deposit")]
    public async Task<IActionResult> SimulateDeposit([FromBody] SimulateDepositRequest request, CancellationToken ct)
    {
        var tenantId = TenantContext.GetTenantId(HttpContext);

        if (request.AmountNaira <= 0)
            throw new Exception("Amount must be positive.");

        var account = await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Reference == request.AccountReference, ct)
            ?? throw new Exception($"Dedicated account '{request.AccountReference}' not found.");

        var simulatedRef = "sim-" + Base64Url(RandomNumberGenerator.GetBytes(12));
        var amountKobo = (long)(request.AmountNaira * 100);

        var parsed = new NombaParsedEvent(
            NombaReference: simulatedRef,
            AccountNumber: account.AccountNumber,
            AmountKobo: amountKobo,
            FeeKobo: 0,
            TransferName: request.SenderName ?? "Sandbox Simulator",
            EventType: request.Reversal ? "payment_reversal" : "payment_success",
            IsReversal: request.Reversal);

        var (status, kredarRef) = await webhook.ReconcileAsync(parsed, ct);

        var updatedAccount = await db.DedicatedAccounts.FindAsync(new object[] { account.Id }, ct);

        return Ok(ApiResponse<SimulatedDepositResponse>.Success(
            new SimulatedDepositResponse("ok", kredarRef, status.ToString(), updatedAccount!.PaymentState.ToString()),
            "Deposit simulated and reconciled successfully."));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
