using ECommerce.Domain.Core.Cart.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartConfiguration : IEntityTypeConfiguration<ECommerce.Domain.Core.Cart.Models.Cart>
{
    public void Configure(EntityTypeBuilder<ECommerce.Domain.Core.Cart.Models.Cart> entity)
    {
        entity.HasKey(cart => cart.Id);

        entity.HasIndex(cart => cart.CustomerId)
            .IsUnique();

        entity.HasMany(cart => cart.Items)
            .WithOne(cartItem => cartItem.Cart)
            .HasForeignKey(cartItem => cartItem.CartId);
    }
}