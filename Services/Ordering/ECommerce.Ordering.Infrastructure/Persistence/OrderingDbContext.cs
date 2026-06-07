using ECommerce.Ordering.Domain.Models;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Infrastructure.Persistence;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(order => new { order.CustomerId, order.IdempotencyKey }).IsUnique();
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

        // TODO: Add CheckoutSagaState DbSet/configuration when the saga state entity is introduced.
        ConfigureOutbox(modelBuilder);
        ConfigureInbox(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.MessageId).HasMaxLength(100).IsRequired();
            entity.Property(message => message.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(message => message.CausationId).HasMaxLength(100).IsRequired();
            entity.Property(message => message.MessageType).HasMaxLength(500).IsRequired();
            entity.Property(message => message.SourceService).HasMaxLength(100).IsRequired();
            entity.Property(message => message.Destination).HasMaxLength(200).IsRequired();
            entity.Property(message => message.Payload).IsRequired();
            entity.Property(message => message.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(message => message.ErrorMessage).HasMaxLength(4000);
            entity.Property(message => message.RowVersion).IsRowVersion();
            entity.HasIndex(message => message.MessageId).IsUnique();
            entity.HasIndex(message => new { message.Status, message.NextRetryAtUtc, message.CreatedAtUtc });
            entity.HasIndex(message => message.CorrelationId);
        });
    }

    private static void ConfigureInbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.MessageId).HasMaxLength(100).IsRequired();
            entity.Property(message => message.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(message => message.CausationId).HasMaxLength(100).IsRequired();
            entity.Property(message => message.MessageType).HasMaxLength(500).IsRequired();
            entity.Property(message => message.ConsumerName).HasMaxLength(200).IsRequired();
            entity.Property(message => message.Payload).IsRequired();
            entity.Property(message => message.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(message => message.ErrorMessage).HasMaxLength(4000);
            entity.Property(message => message.RowVersion).IsRowVersion();
            entity.HasIndex(message => new { message.MessageId, message.ConsumerName }).IsUnique();
            entity.HasIndex(message => new { message.Status, message.NextRetryAtUtc, message.ReceivedAtUtc });
            entity.HasIndex(message => message.CorrelationId);
        });
    }
}
