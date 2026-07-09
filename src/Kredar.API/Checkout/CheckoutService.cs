using System.Security.Cryptography;
using Kredar.API.Data;
using Kredar.API.DedicatedAccounts;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Checkout;

public record CheckoutSnapshot(
    string AccountReference,
    string AccountNumber,
    string BankName,
    string AccountName,
    string PaymentState,
    decimal AmountPaid,
    decimal? ExpectedAmount);

public class CheckoutService(AppDbContext db)
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxTtl = TimeSpan.FromDays(1);

    public async Task<(CheckoutSession Session, DedicatedAccount Account)> CreateSessionAsync(
        Guid tenantId, string accountReference, int? ttlSeconds)
    {
        var account = await db.DedicatedAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Reference == accountReference)
            ?? throw new Exception($"Dedicated account '{accountReference}' not found.");

        var ttl = ttlSeconds is int s && s > 0
            ? TimeSpan.FromSeconds(Math.Min(s, (int)MaxTtl.TotalSeconds))
            : DefaultTtl;

        var token = "chk_" + Base64Url(RandomNumberGenerator.GetBytes(24));
        var session = new CheckoutSession
        {
            TenantId = tenantId,
            DedicatedAccountId = account.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };

        db.CheckoutSessions.Add(session);
        await db.SaveChangesAsync();
        return (session, account);
    }

    public async Task<(CheckoutSession Session, DedicatedAccount Account)?> ResolveAsync(string token)
    {
        var session = await db.CheckoutSessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session == null || session.IsExpired) return null;

        var account = await db.DedicatedAccounts.FindAsync(session.DedicatedAccountId);
        return account == null ? null : (session, account);
    }

    public static CheckoutSnapshot Snapshot(DedicatedAccount a) => new(
        a.Reference, a.AccountNumber, a.BankName, a.AccountName,
        a.PaymentState.ToString(), a.AmountPaid, a.ExpectedAmount);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
