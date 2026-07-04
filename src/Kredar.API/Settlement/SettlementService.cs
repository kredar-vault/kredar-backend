using Kredar.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Settlement;

public class SettlementService(AppDbContext db)
{
    // --- Settlement Config ---

    public async Task<SettlementConfig> GetConfigAsync(Guid tenantId, CancellationToken ct = default)
    {
        var config = await db.SettlementConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);
        if (config != null) return config;
        config = new SettlementConfig { TenantId = tenantId };
        db.SettlementConfigs.Add(config);
        await db.SaveChangesAsync(ct);
        return config;
    }

    public async Task<SettlementConfig> UpdateConfigAsync(Guid tenantId,
        string? accountNumber, string? bankCode, string? accountName,
        bool autoSettle, decimal minPayoutNaira, CancellationToken ct = default)
    {
        var config = await GetConfigAsync(tenantId, ct);
        config.SettlementAccountNumber = accountNumber;
        config.SettlementBankCode = bankCode;
        config.SettlementAccountName = accountName;
        config.AutoSettle = autoSettle;
        config.MinPayoutNaira = minPayoutNaira;
        config.UpdatedAt = DateTime.UtcNow;
        db.SettlementConfigs.Update(config);
        await db.SaveChangesAsync(ct);
        return config;
    }

    // --- Settlement Splits ---

    public async Task<List<SettlementSplit>> GetSplitsAsync(Guid tenantId, CancellationToken ct = default) =>
        await db.SettlementSplits.Where(s => s.TenantId == tenantId).OrderBy(s => s.Priority).ToListAsync(ct);

    public async Task<List<SettlementSplit>> SetSplitsAsync(Guid tenantId, List<SplitSpec> specs, CancellationToken ct = default)
    {
        var existing = await db.SettlementSplits.Where(s => s.TenantId == tenantId).ToListAsync(ct);
        db.SettlementSplits.RemoveRange(existing);

        var splits = specs.Select((s, i) => new SettlementSplit
        {
            TenantId = tenantId,
            BeneficiaryName = s.BeneficiaryName,
            AccountNumber = s.AccountNumber,
            BankCode = s.BankCode,
            Basis = s.Basis.Equals("Flat", StringComparison.OrdinalIgnoreCase) ? SplitBasis.Flat : SplitBasis.Percentage,
            ShareBps = s.ShareBps,
            FlatNaira = s.FlatNaira,
            Priority = i
        }).ToList();

        db.SettlementSplits.AddRange(splits);
        await db.SaveChangesAsync(ct);
        return splits;
    }

    // --- Escrow ---

    public async Task<EscrowHold> HoldAsync(Guid tenantId, string accountReference, string? releaseCondition, CancellationToken ct = default)
    {
        var account = await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Reference == accountReference, ct)
            ?? throw new Exception($"Dedicated account '{accountReference}' not found.");

        var existing = await db.EscrowHolds
            .FirstOrDefaultAsync(h => h.DedicatedAccountId == account.Id && h.State == EscrowState.Held, ct);
        if (existing != null)
            throw new Exception("This account already has an active escrow hold.");

        var hold = new EscrowHold
        {
            TenantId = tenantId,
            DedicatedAccountId = account.Id,
            AmountNaira = account.AmountPaid,
            ReleaseCondition = releaseCondition
        };

        db.EscrowHolds.Add(hold);
        await db.SaveChangesAsync(ct);
        return hold;
    }

    public async Task ReleaseAsync(Guid tenantId, string accountReference, CancellationToken ct = default)
    {
        var account = await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Reference == accountReference, ct)
            ?? throw new Exception($"Dedicated account '{accountReference}' not found.");

        var hold = await db.EscrowHolds
            .FirstOrDefaultAsync(h => h.DedicatedAccountId == account.Id && h.State == EscrowState.Held, ct)
            ?? throw new Exception("No active escrow hold found for this account.");

        hold.State = EscrowState.Released;
        hold.ReleasedAt = DateTime.UtcNow;
        db.EscrowHolds.Update(hold);
        await db.SaveChangesAsync(ct);
    }
}

public record SplitSpec(string BeneficiaryName, string AccountNumber, string BankCode, string Basis, int ShareBps, decimal FlatNaira);
