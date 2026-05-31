using ECommerce.Domain.Core.Cart.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> entity)
    {
        entity.HasKey(cartItem => cartItem.Id);

        entity.Property(cartItem => cartItem.CartId)
            .IsRequired();

        entity.Property(cartItem => cartItem.ProductId)
            .IsRequired();

        entity.Property(cartItem => cartItem.ProductNameSnapshot)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(cartItem => cartItem.ProductImageUrlSnapshot)
            .HasMaxLength(1000);

        entity.Property(cartItem => cartItem.UnitPriceSnapshot)
            .HasPrecision(18, 2)
            .IsRequired();

        entity.Property(cartItem => cartItem.Quantity)
            .IsRequired();

        entity.Ignore(cartItem => cartItem.LineTotal);

        entity.HasIndex(cartItem => new
        {
            cartItem.CartId,
            cartItem.ProductId
        })
        .IsUnique();
    }
}