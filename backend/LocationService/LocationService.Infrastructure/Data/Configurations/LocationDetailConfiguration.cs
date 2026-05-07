using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LocationService.Domain.Entities;

namespace LocationService.Infrastructure.Data.Configurations
{
    public class LocationDetailConfiguration : IEntityTypeConfiguration<LocationDetail>
    {
        public void Configure(EntityTypeBuilder<LocationDetail> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.LocationId).IsRequired();

            builder.Property(d => d.PropertyName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.PropertyValue)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasOne(d => d.Location)
                .WithMany(l => l.Details)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(d => new { d.LocationId, d.PropertyName }).IsUnique();
        }
    }
}
