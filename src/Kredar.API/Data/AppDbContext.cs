using Kredar.API.Tenants;
using Microsoft.EntityFrameworkCore;

namespace Kredar.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Email).IsUnique();
            e.Property(t => t.Email).IsRequired();
            e.Property(t => t.BusinessName).IsRequired();
        });
    }
}
