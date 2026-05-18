using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Infrastructure.Data.Configurations;

public class DiscoveryRunConfiguration : IEntityTypeConfiguration<DiscoveryRun>
{
    public void Configure(EntityTypeBuilder<DiscoveryRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.HromadaId).IsRequired();
        builder.Property(r => r.StartedAt).IsRequired();
        builder.Property(r => r.Error).HasMaxLength(2000);
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasOne(r => r.Hromada)
            .WithMany(h => h.DiscoveryRuns)
            .HasForeignKey(r => r.HromadaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
