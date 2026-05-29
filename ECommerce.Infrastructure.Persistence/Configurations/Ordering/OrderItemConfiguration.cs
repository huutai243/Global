using ECommerce.Domain.Core.Ordering.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Ordering;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.HasKey(orderItem => orderItem.Id);

        entity.Property(orderItem => orderItem.ProductNameSnapshot)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(orderItem => orderItem.UnitPriceSnapshot)
            .HasPrecision(18, 2);

        entity.Property(orderItem => orderItem.LineTotal)
            .HasPrecision(18, 2);
    }
}