using ECommerce.Shared.Inbox;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using PaymentEntity = ECommerce.Payment.Domain.Models.Payment;

namespace ECommerce.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(payment => payment.Provider).HasMaxLength(100).IsRequired();
            entity.Property(payment => payment.ProviderTransactionId).HasMaxLength(200);
            // IDEMPOTENCY NOTE:
            // This is not a unique idempotency guard. Payment exactly-once business effect
            // still needs a persisted idempotency key or provider transaction uniqueness strategy.
            entity.HasIndex(payment => payment.OrderId);
        });

        ConfigureOutbox(modelBuilder);
        ConfigureInbox(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            // AUDIT NOTE:
            // Outbox/Inbox provide integration trace, but not a full business audit trail.
            // A real audit trail should record actor, action, entity id, old value, new value, correlation id, and timestamp.
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
            // IDEMPOTENCY NOTE:
            // Unique MessageId protects stored integration records, but broker delivery may still duplicate messages.
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
            // IDEMPOTENCY NOTE:
            // MessageId + ConsumerName is the durable duplicate-detection key for at-least-once consumers.
            entity.HasIndex(message => new { message.MessageId, message.ConsumerName }).IsUnique();
            entity.HasIndex(message => new { message.Status, message.NextRetryAtUtc, message.ReceivedAtUtc });
            entity.HasIndex(message => message.CorrelationId);
        });
    }
}
