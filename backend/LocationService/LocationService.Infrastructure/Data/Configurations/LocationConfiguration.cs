using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LocationService.Domain.Entities;

namespace LocationService.Infrastructure.Data.Configurations
{
    public class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Latitude)
                .IsRequired();

            builder.Property(l => l.Longitude)
                .IsRequired();

            builder.Property(l => l.Address)
                .HasMaxLength(200);

            builder.HasMany(l => l.Details)
                .WithOne(d => d.Location)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Soft-delete fields
            builder.Property(l => l.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(l => l.DeletedAt);
            builder.Property(l => l.DeletedByUserId);

            // Optimistic concurrency: map to PostgreSQL system column "xmin"
            builder.Property(l => l.RowVersion)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Global query filter — every read excludes soft-deleted rows by default.
            // To bypass: .IgnoreQueryFilters() in a specific query (admin tooling only).
            builder.HasQueryFilter(l => !l.IsDeleted);
        }
    }
}
