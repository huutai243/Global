using ECommerce.Cart.Domain.Models;
using ECommerce.Catalog.Domain.Models;
using ECommerce.Identity.Domain.Models;
using ECommerce.Inventory.Domain.Models;
using ECommerce.Ordering.Domain.Models;
using ECommerce.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

// TODO: Legacy shared DbContext. Replace with service-specific DbContexts and remove after migration.
public class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockReservation> StockReservation => Set<StockReservation>();
    public DbSet<StockReservationItem> StockReservationItem => Set<StockReservationItem>();


    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<ECommerce.Cart.Domain.Models.Cart> Carts => Set<ECommerce.Cart.Domain.Models.Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceDbContext).Assembly);
    }
}
