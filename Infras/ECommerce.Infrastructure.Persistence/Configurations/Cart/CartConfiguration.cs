using ECommerce.Cart.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartConfiguration : IEntityTypeConfiguration<ECommerce.Cart.Domain.Models.Cart>
{
    public void Configure(EntityTypeBuilder<ECommerce.Cart.Domain.Models.Cart> entity)
    {
        entity.HasKey(cart => cart.Id);

        entity.Property(cart => cart.CustomerId)
            .IsRequired();

        entity.Property(cart => cart.CreatedAt)
            .IsRequired();

        entity.Property(cart => cart.UpdatedAt);

        entity.HasIndex(cart => cart.CustomerId)
            .IsUnique();

        entity.HasMany(cart => cart.Items)
            .WithOne(cartItem => cartItem.Cart)
            .HasForeignKey(cartItem => cartItem.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}