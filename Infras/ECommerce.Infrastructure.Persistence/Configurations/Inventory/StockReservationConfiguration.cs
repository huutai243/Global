using ECommerce.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.OrderId).IsUnique();

        entity.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.FailureReason).HasMaxLength(500);

        entity.HasMany(x => x.Items)
            .WithOne(x => x.StockReservation)
            .HasForeignKey(x => x.StockReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}