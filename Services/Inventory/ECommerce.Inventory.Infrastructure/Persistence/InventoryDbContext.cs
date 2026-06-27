using ECommerce.Inventory.Domain.Models;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<StockReservationItem> StockReservationItems => Set<StockReservationItem>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.ProductId).IsUnique();
            // STRONG CONSISTENCY NOTE:
            // RowVersion gives EF optimistic concurrency for stock rows.
            // Reservation handlers must treat conflicts as retryable to avoid lost updates and oversell.
            builder.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<StockReservation>(builder =>
        {
            builder.HasKey(x => x.Id);
            // IDEMPOTENCY NOTE:
            // One StockReservation per OrderId prevents duplicate ReserveInventoryCommand delivery
            // from creating multiple reservations or decrementing stock more than once.
            builder.HasIndex(x => x.OrderId).IsUnique();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(500);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.StockReservation)
                .HasForeignKey(x => x.StockReservationId);
        });

        modelBuilder.Entity<StockReservationItem>(builder =>
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.StockReservationId, x.ProductId })
                .IsUnique();

            builder.Property(x => x.ProductNameSnapshot)
                .HasMaxLength(300)
                .IsRequired();
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);

            // IDEMPOTENCY NOTE:
            // MessageId + ConsumerName is the durable duplicate-detection key for at-least-once delivery.
            builder.HasIndex(x => new { x.MessageId, x.ConsumerName })
                .IsUnique();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);

            // AUDIT NOTE:
            // Outbox/Inbox provide integration trace, but not a full business audit trail.
            // A real audit trail should record actor, action, entity id, old value, new value, correlation id, and timestamp.
            builder.HasIndex(x => x.MessageId)
                .IsUnique();

            builder.HasIndex(x => new { x.Status, x.NextRetryAtUtc, x.CreatedAtUtc });
            builder.HasIndex(x => new { x.Status, x.ProcessingStartedAtUtc });

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        base.OnModelCreating(modelBuilder);
    }
}
