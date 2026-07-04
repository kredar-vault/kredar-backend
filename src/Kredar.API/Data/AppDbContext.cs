using Kredar.API.Auth;
using Kredar.API.Customers;
using Kredar.API.DedicatedAccounts;
using Kredar.API.Team;
using Kredar.API.Tenants;
using Kredar.API.Transactions;
using Kredar.API.Transfers;
using Kredar.API.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerKycDocument> CustomerKycDocuments => Set<CustomerKycDocument>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<DedicatedAccount> DedicatedAccounts => Set<DedicatedAccount>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Email).IsUnique();
            e.Property(t => t.Email).IsRequired();
            e.Property(t => t.BusinessName).IsRequired(false);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Token).IsUnique();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.TenantId, c.Email }).IsUnique();
            e.Property(c => c.Email).IsRequired();
            e.Property(c => c.KycStatus).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();
        });

        modelBuilder.Entity<CustomerKycDocument>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.DocumentType).HasConversion<string>();
            e.Property(k => k.Status).HasConversion<string>();
            e.Property(k => k.FileUrl).IsRequired();
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Reference).IsUnique();
            e.Property(t => t.Status).HasConversion<string>();
            e.Property(t => t.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TeamMember>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.TenantId, t.Email }).IsUnique();
            e.Property(t => t.Role).HasConversion<string>();
        });

        modelBuilder.Entity<DedicatedAccount>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.AccountNumber).IsUnique();
            e.HasIndex(a => new { a.TenantId, a.Reference }).IsUnique();
            e.Property(a => a.Status).HasConversion<string>();
            e.Property(a => a.PaymentState).HasConversion<string>();
            e.Property(a => a.AmountPaid).HasPrecision(18, 2);
            e.Property(a => a.ExpectedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Transfer>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.TenantId, t.MerchantTxRef }).IsUnique();
            e.Property(t => t.Status).HasConversion<string>();
            e.Property(t => t.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<WebhookEndpoint>(e =>
        {
            e.HasKey(ep => ep.Id);
            e.Property(ep => ep.Url).HasMaxLength(2048);
            e.Property(ep => ep.SigningSecret).HasMaxLength(128);
        });

        modelBuilder.Entity<WebhookDelivery>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Status).HasConversion<string>();
            e.HasIndex(d => new { d.Status, d.NextAttemptAt });
        });
    }
}
