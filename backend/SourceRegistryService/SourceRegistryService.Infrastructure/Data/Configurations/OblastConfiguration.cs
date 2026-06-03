using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Infrastructure.Data.Configurations;

public class OblastConfiguration : IEntityTypeConfiguration<Oblast>
{
    public void Configure(EntityTypeBuilder<Oblast> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Code).IsRequired().HasMaxLength(10);
        builder.HasIndex(o => o.Code).IsUnique();
        builder.Property(o => o.KatottgCode).HasMaxLength(30);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.NameUk).IsRequired().HasMaxLength(200);
        builder.Property(o => o.IsOccupied).IsRequired().HasDefaultValue(false);
        builder.Property(o => o.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(o => o.DeletedAt);
        builder.Property(o => o.DeletedByUserId);
        builder.Property(o => o.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasMany(o => o.Hromadas)
            .WithOne(h => h.Oblast)
            .HasForeignKey(h => h.OblastId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
