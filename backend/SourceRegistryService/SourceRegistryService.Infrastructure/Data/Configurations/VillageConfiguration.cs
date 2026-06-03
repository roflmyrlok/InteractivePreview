using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Infrastructure.Data.Configurations;

public class VillageConfiguration : IEntityTypeConfiguration<Village>
{
    public void Configure(EntityTypeBuilder<Village> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.NameUk).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Slug).IsRequired().HasMaxLength(100);
        builder.Property(v => v.KatottgCode).HasMaxLength(30);
        builder.HasIndex(v => new { v.HromadaId, v.Slug }).IsUnique();
        builder.Property(v => v.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(v => v.DeletedAt);
        builder.Property(v => v.DeletedByUserId);
        builder.Property(v => v.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        // DataSource uses polymorphic ScopeType+ScopeId — no EF FK here.
        builder.Ignore(v => v.DataSources);
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
