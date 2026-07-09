using Kredar.API.Data;
using Kredar.API.Nomba;
using Kredar.API.Transfers;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Settlement;

/// <summary>
/// Runs every 30 seconds. For each tenant with AutoSettle=true, finds DVAs that have
/// collected more than they've settled out, and initiates a transfer to the configured
/// settlement bank account. Idempotent — uses a stable merchantTxRef per DVA round.
/// </summary>
public sealed class SettlementWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SettlementWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "Settlement worker failed."); }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nomba = scope.ServiceProvider.GetRequiredService<NombaClient>();

        var configs = await db.SettlementConfigs
            .Where(c => c.AutoSettle
                && !string.IsNullOrEmpty(c.SettlementAccountNumber)
                && !string.IsNullOrEmpty(c.SettlementBankCode))
            .ToListAsync(ct);

        if (configs.Count == 0) return;

        foreach (var config in configs)
        {
            var accounts = await db.DedicatedAccounts
                .Where(a => a.TenantId == config.TenantId
                    && a.Status == DedicatedAccounts.DedicatedAccountStatus.Active
                    && a.AmountPaid > 0)
                .ToListAsync(ct);

            foreach (var account in accounts)
            {
                // Sum all successful settlement transfers already made for this DVA
                var prefix = $"settle-{account.Id:N}-";
                var alreadySettled = await db.Transfers
                    .Where(t => t.TenantId == config.TenantId
                        && t.MerchantTxRef.StartsWith(prefix)
                        && t.Status == TransferStatus.Succeeded)
                    .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

                var unsettled = account.AmountPaid - alreadySettled;
                if (unsettled < config.MinPayoutNaira || unsettled <= 0) continue;

                // Stable idempotency key — keyed on the current settled watermark
                var roundKey = (long)(alreadySettled * 100);
                var merchantRef = $"settle-{account.Id:N}-{roundKey}";

                var existingTransfer = await db.Transfers
                    .FirstOrDefaultAsync(t => t.MerchantTxRef == merchantRef, ct);
                if (existingTransfer != null) continue;

                logger.LogInformation("Auto-settling {AccountRef}: ₦{Amount} -> {Account}",
                    account.Reference, unsettled, config.SettlementAccountNumber);

                var transfer = new Transfer
                {
                    TenantId = config.TenantId,
                    MerchantTxRef = merchantRef,
                    Amount = unsettled,
                    RecipientAccountNumber = config.SettlementAccountNumber!,
                    RecipientBankCode = config.SettlementBankCode!,
                    RecipientName = config.SettlementAccountName,
                    Narration = $"Kredar settlement for {account.Reference}",
                };
                db.Transfers.Add(transfer);
                await db.SaveChangesAsync(ct);

                var result = await nomba.InitiateTransferAsync(
                    merchantRef, unsettled,
                    config.SettlementAccountNumber!, config.SettlementBankCode!,
                    config.SettlementAccountName, transfer.Narration, ct);

                if (result.Success)
                {
                    transfer.Status = TransferStatus.Succeeded;
                    transfer.ProviderReference = result.Reference;
                    transfer.CompletedAt = DateTime.UtcNow;
                    logger.LogInformation("Settlement succeeded for {AccountRef}: ref={Ref}", account.Reference, result.Reference);
                }
                else
                {
                    transfer.Status = TransferStatus.Failed;
                    transfer.FailureReason = result.Error;
                    transfer.CompletedAt = DateTime.UtcNow;
                    logger.LogWarning("Settlement failed for {AccountRef}: {Reason}", account.Reference, result.Error);
                }

                await db.SaveChangesAsync(ct);
            }
        }
    }
}
