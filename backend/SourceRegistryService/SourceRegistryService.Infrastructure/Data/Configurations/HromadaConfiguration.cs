using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Infrastructure.Data.Configurations;

public class HromadaConfiguration : IEntityTypeConfiguration<Hromada>
{
    public void Configure(EntityTypeBuilder<Hromada> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Name).IsRequired().HasMaxLength(200);
        builder.Property(h => h.NameUk).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(h => new { h.OblastId, h.Slug }).IsUnique();
        builder.Property(h => h.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(h => h.DeletedAt);
        builder.Property(h => h.DeletedByUserId);
        builder.Property(h => h.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        // DataSource uses polymorphic ScopeType+ScopeId — no EF FK here.
        // Repositories filter by ScopeType+ScopeId directly.
        builder.Ignore(h => h.DataSources);
        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}
