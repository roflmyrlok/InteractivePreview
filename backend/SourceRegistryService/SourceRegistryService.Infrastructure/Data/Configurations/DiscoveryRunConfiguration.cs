using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Infrastructure.Data.Configurations;

public class DiscoveryRunConfiguration : IEntityTypeConfiguration<DiscoveryRun>
{
    public void Configure(EntityTypeBuilder<DiscoveryRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ScopeType).IsRequired();
        builder.Property(r => r.ScopeId).IsRequired();
        builder.HasIndex(r => new { r.ScopeType, r.ScopeId });
        builder.Property(r => r.StartedAt).IsRequired();
        builder.Property(r => r.Error).HasMaxLength(2000);
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
