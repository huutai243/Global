using ECommerce.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockReservationItemConfiguration : IEntityTypeConfiguration<StockReservationItem>
{
    public void Configure(EntityTypeBuilder<StockReservationItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.ProductNameSnapshot)
            .HasMaxLength(300)
            .IsRequired();

        entity.Property(x => x.Quantity).IsRequired();

        entity.HasIndex(x => x.ProductId);
    }
}