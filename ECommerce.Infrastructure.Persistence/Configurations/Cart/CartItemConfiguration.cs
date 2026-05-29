using ECommerce.Domain.Core.Cart.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> entity)
    {
        entity.HasKey(cartItem => cartItem.Id);

        entity.Property(cartItem => cartItem.ProductNameSnapshot)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(cartItem => cartItem.UnitPriceSnapshot)
            .HasPrecision(18, 2);

        entity.Property(cartItem => cartItem.LineTotal)
            .HasPrecision(18, 2);
    }
}