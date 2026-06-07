using ECommerce.Cart.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Cart.Infrastructure.Persistence;

public sealed class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options)
{
    public DbSet<Cart.Domain.Models.Cart> Carts => Set<Cart.Domain.Models.Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart.Domain.Models.Cart>(entity =>
        {
            entity.HasKey(cart => cart.Id);
            entity.HasIndex(cart => cart.CustomerId).IsUnique();
            entity.HasMany(cart => cart.Items)
                .WithOne(cartItem => cartItem.Cart)
                .HasForeignKey(cartItem => cartItem.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(cartItem => cartItem.Id);
            entity.Property(cartItem => cartItem.ProductNameSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(cartItem => cartItem.UnitPriceSnapshot).HasPrecision(18, 2);
            entity.HasIndex(cartItem => new { cartItem.CartId, cartItem.ProductId }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
