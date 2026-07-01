using Kredar.API.Auth;
using Kredar.API.Customers;
using Kredar.API.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Customer> Customers => Set<Customer>();

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
    }
}
