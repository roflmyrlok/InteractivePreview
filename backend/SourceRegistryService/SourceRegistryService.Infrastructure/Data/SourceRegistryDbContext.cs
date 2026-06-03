using Microsoft.EntityFrameworkCore;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Infrastructure.Data.Configurations;

namespace SourceRegistryService.Infrastructure.Data;

public class SourceRegistryDbContext : DbContext
{
    public SourceRegistryDbContext(DbContextOptions<SourceRegistryDbContext> options)
        : base(options) { }

    public DbSet<Oblast> Oblasts { get; set; }
    public DbSet<Hromada> Hromadas { get; set; }
    public DbSet<Village> Villages { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<DiscoveryRun> DiscoveryRuns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OblastConfiguration());
        modelBuilder.ApplyConfiguration(new HromadaConfiguration());
        modelBuilder.ApplyConfiguration(new VillageConfiguration());
        modelBuilder.ApplyConfiguration(new DataSourceConfiguration());
        modelBuilder.ApplyConfiguration(new DiscoveryRunConfiguration());

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Oblast>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        foreach (var entry in ChangeTracker.Entries<Hromada>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        foreach (var entry in ChangeTracker.Entries<Village>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        foreach (var entry in ChangeTracker.Entries<DataSource>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
