using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Infrastructure.Data.Configurations;

public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.HasKey(ds => ds.Id);
        builder.Property(ds => ds.ScopeType).IsRequired();
        builder.Property(ds => ds.ScopeId).IsRequired();
        builder.Property(ds => ds.Url).IsRequired().HasMaxLength(1000);
        builder.Property(ds => ds.Description).HasMaxLength(500);
        builder.Property(ds => ds.Status).IsRequired();
        builder.Property(ds => ds.Origin).IsRequired();
        builder.Property(ds => ds.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(ds => ds.DeletedAt);
        builder.Property(ds => ds.DeletedByUserId);
        builder.Property(ds => ds.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        // Unique URL per scope (includes rejected — prevents AI re-suggestion)
        builder.HasIndex(ds => new { ds.Url, ds.ScopeType, ds.ScopeId }).IsUnique();
        builder.HasQueryFilter(ds => !ds.IsDeleted);
    }
}
