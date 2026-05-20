using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Cart.Models;
using ECommerce.Domain.Core.Identity.Models;
using ECommerce.Infrastructure.Persistence.Models;
using ECommerce.Domain.Core.Inventory.Models;
using ECommerce.Domain.Core.Ordering.Models;
using ECommerce.Domain.Core.Payment.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<ECommerce.Domain.Core.Payment.Models.Payment> Payments => Set<ECommerce.Domain.Core.Payment.Models.Payment>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(200).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(1000);
            entity.HasMany(category => category.Products)
                .WithOne(product => product.Category)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(2000);
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(product => product.CategoryId);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(inventoryItem => inventoryItem.Id);
            entity.HasIndex(inventoryItem => inventoryItem.ProductId).IsUnique();
            entity.Property(inventoryItem => inventoryItem.RowVersion).IsRowVersion();
            entity.HasOne<Product>()
                .WithOne()
                .HasForeignKey<InventoryItem>(inventoryItem => inventoryItem.ProductId);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Reason).HasMaxLength(500).IsRequired();
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(transaction => transaction.ProductId);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.FullName).HasMaxLength(200).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(50).IsRequired();
            entity.HasOne(user => user.Customer)
                .WithOne(customer => customer.User)
                .HasForeignKey<Customer>(customer => customer.UserId);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.FullName).HasMaxLength(200).IsRequired();
            entity.Property(customer => customer.Email).HasMaxLength(320).IsRequired();
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(cart => cart.Id);
            entity.HasIndex(cart => cart.CustomerId).IsUnique();
            entity.HasMany(cart => cart.Items)
                .WithOne(cartItem => cartItem.Cart)
                .HasForeignKey(cartItem => cartItem.CartId);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(cartItem => cartItem.Id);
            entity.Property(cartItem => cartItem.ProductNameSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(cartItem => cartItem.UnitPriceSnapshot).HasPrecision(18, 2);
            entity.Property(cartItem => cartItem.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasMany(order => order.Items)
                .WithOne(orderItem => orderItem.Order)
                .HasForeignKey(orderItem => orderItem.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(orderItem => orderItem.Id);
            entity.Property(orderItem => orderItem.ProductNameSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(orderItem => orderItem.UnitPriceSnapshot).HasPrecision(18, 2);
            entity.Property(orderItem => orderItem.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ECommerce.Domain.Core.Payment.Models.Payment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(payment => payment.Provider).HasMaxLength(100).IsRequired();
            entity.Property(payment => payment.ProviderTransactionId).HasMaxLength(200);
            entity.HasOne<Order>()
                .WithMany()
                .HasForeignKey(payment => payment.OrderId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(outboxMessage => outboxMessage.Id);
            entity.Property(outboxMessage => outboxMessage.EventType).HasMaxLength(300).IsRequired();
            entity.Property(outboxMessage => outboxMessage.Payload).IsRequired();
            entity.Property(outboxMessage => outboxMessage.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(outboxMessage => outboxMessage.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(outboxMessage => new { outboxMessage.Status, outboxMessage.NextRetryAt });
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(idempotencyRecord => idempotencyRecord.Id);
            entity.HasIndex(idempotencyRecord => idempotencyRecord.Key).IsUnique();
            entity.Property(idempotencyRecord => idempotencyRecord.Key).HasMaxLength(200).IsRequired();
            entity.Property(idempotencyRecord => idempotencyRecord.RequestHash).HasMaxLength(500).IsRequired();
            entity.Property(idempotencyRecord => idempotencyRecord.ResponsePayload).HasMaxLength(4000);
            entity.Property(idempotencyRecord => idempotencyRecord.Status).HasMaxLength(50).IsRequired();
        });
    }
}
