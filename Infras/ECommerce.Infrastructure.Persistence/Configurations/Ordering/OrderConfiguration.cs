using ECommerce.Ordering.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Ordering;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.HasKey(order => order.Id);

        entity.Property(order => order.TotalAmount)
            .HasPrecision(18, 2);

        entity.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.HasMany(order => order.Items)
            .WithOne(orderItem => orderItem.Order)
            .HasForeignKey(orderItem => orderItem.OrderId);
    }
}