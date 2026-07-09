using Kredar.API.Admin;
using Kredar.API.ApiKeys;
using Kredar.API.Auth;
using Kredar.API.Billing;
using Kredar.API.Notifications;
using Kredar.API.Checkout;
using Kredar.API.Customers;
using Kredar.API.DedicatedAccounts;
using Kredar.API.Onboarding;
using Kredar.API.Rules;
using Kredar.API.Settlement;
using Kredar.API.SubMerchants;
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
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();
    public DbSet<SettlementConfig> SettlementConfigs => Set<SettlementConfig>();
    public DbSet<SettlementSplit> SettlementSplits => Set<SettlementSplit>();
    public DbSet<EscrowHold> EscrowHolds => Set<EscrowHold>();
    public DbSet<MoneyRule> MoneyRules => Set<MoneyRule>();
    public DbSet<SubMerchant> SubMerchants => Set<SubMerchant>();
    public DbSet<BillingSchedule> BillingSchedules => Set<BillingSchedule>();
    public DbSet<BillingPeriod> BillingPeriods => Set<BillingPeriod>();
    public DbSet<OnboardingDocument> OnboardingDocuments => Set<OnboardingDocument>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OnboardingApplication> OnboardingApplications => Set<OnboardingApplication>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

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

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasKey(k => k.Id);
            e.HasIndex(k => k.ClientId).IsUnique();
            e.Property(k => k.Mode).HasConversion<string>();
            e.Property(k => k.Status).HasConversion<string>();
        });

        modelBuilder.Entity<CheckoutSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.Token).IsUnique();
        });

        modelBuilder.Entity<SettlementConfig>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.TenantId).IsUnique();
            e.Property(c => c.MinPayoutNaira).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SettlementSplit>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Basis).HasConversion<string>();
            e.Property(s => s.FlatNaira).HasPrecision(18, 2);
        });

        modelBuilder.Entity<EscrowHold>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.State).HasConversion<string>();
            e.Property(h => h.AmountNaira).HasPrecision(18, 2);
        });

        modelBuilder.Entity<MoneyRule>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Trigger).HasConversion<string>();
            e.Property(r => r.Action).HasConversion<string>();
            e.Property(r => r.ThresholdNaira).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SubMerchant>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.TenantId, s.Reference }).IsUnique();
            e.Property(s => s.Status).HasConversion<string>();
        });

        modelBuilder.Entity<OnboardingApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.TenantId).IsUnique();
            e.Property(a => a.Status).HasConversion<string>();
            e.Property(a => a.Tier).HasConversion<string>();
            e.Property(a => a.DeveloperKycStatus).HasConversion<string>();
            e.Property(a => a.BusinessKybStatus).HasConversion<string>();
            e.Property(a => a.DevGovIdType).HasConversion<string>();
            e.HasOne(a => a.Tenant).WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(a => a.Documents).WithOne().HasForeignKey(d => d.OnboardingApplicationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OnboardingDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.DocumentType).HasConversion<string>();
        });

        modelBuilder.Entity<TeamMember>(e =>
        {
            e.Property(t => t.Status).HasConversion<string>();
        });

        modelBuilder.Entity<BillingSchedule>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.TenantId, s.Reference }).IsUnique().HasFilter("\"Reference\" IS NOT NULL");
            e.Property(s => s.Interval).HasConversion<string>();
            e.Property(s => s.Status).HasConversion<string>();
            e.HasMany(s => s.Periods).WithOne(p => p.Schedule).HasForeignKey(p => p.ScheduleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BillingPeriod>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).HasConversion<string>();
            e.Ignore(p => p.OutstandingKobo);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => new { n.TenantId, n.CreatedAt });
            e.Property(n => n.Type).HasConversion<string>();
        });

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.Email).IsUnique();
            e.Property(a => a.Role).HasConversion<string>();
        });

        modelBuilder.Entity<AdminAuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.CreatedAt);
        });
    }
}
