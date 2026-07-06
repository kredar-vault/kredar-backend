using Kredar.API.Data;
using Kredar.API.Nomba;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Webhooks;

/// <summary>
/// Polls Nomba's /transactions API every 5 minutes and reconciles any deposits
/// that were missed by the inbound webhook (e.g. webhook secret mismatch, network failure).
/// </summary>
public sealed class ReconciliationSweepWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReconciliationSweepWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    // Look back 15 minutes to cover any clock skew or delay
    private static readonly TimeSpan LookbackWindow = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay startup by 30s so the DB migration completes first
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SweepAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "Reconciliation sweep failed."); }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var nomba = scope.ServiceProvider.GetRequiredService<NombaClient>();
        var webhookService = scope.ServiceProvider.GetRequiredService<NombaWebhookService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var to = DateTime.UtcNow;
        var from = to - LookbackWindow;

        List<NombaTransactionRecord> nombaTransactions;
        try { nombaTransactions = await nomba.GetRecentTransactionsAsync(from, to, ct); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reconciliation sweep: failed to fetch Nomba transactions.");
            return;
        }

        if (nombaTransactions.Count == 0) return;

        // Load all active DVA account numbers into a hash set for fast lookup
        var activeAccountNumbers = await db.DedicatedAccounts
            .Where(a => a.Status == DedicatedAccounts.DedicatedAccountStatus.Active)
            .Select(a => a.AccountNumber)
            .ToHashSetAsync(ct);

        int reconciled = 0;
        foreach (var tx in nombaTransactions)
        {
            if (!activeAccountNumbers.Contains(tx.AccountNumber)) continue;

            // Skip if already reconciled
            var exists = await db.Transactions.AnyAsync(t => t.PaymentReference == tx.Reference, ct);
            if (exists) continue;

            try
            {
                var parsed = new NombaParsedEvent(
                    tx.Reference, tx.AccountNumber, tx.AmountKobo, tx.FeeKobo,
                    tx.SenderName, "virtual_account.funded", false);

                await webhookService.ReconcileAsync(parsed, ct);
                reconciled++;
                logger.LogInformation(
                    "Sweep reconciled missed transaction: Ref={Ref} Account={Account} Amount={Amount}kobo",
                    tx.Reference, tx.AccountNumber, tx.AmountKobo);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Sweep: failed to reconcile Ref={Ref}.", tx.Reference);
            }
        }

        if (reconciled > 0)
            logger.LogInformation("Reconciliation sweep complete: {Count} missed transaction(s) recovered.", reconciled);
    }
}
