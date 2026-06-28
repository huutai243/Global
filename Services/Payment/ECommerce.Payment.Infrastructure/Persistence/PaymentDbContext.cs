using ECommerce.Payment.Domain.Models;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurePaymentTransactions(modelBuilder);
        ConfigureOutbox(modelBuilder);
        ConfigureInbox(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigurePaymentTransactions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");

            entity.HasKey(payment => payment.Id);

            // EXACTLY-ONCE BUSINESS EFFECT NOTE:
            // One payment transaction per OrderId prevents duplicate PayOrderCommand delivery
            // from charging or recording the same order more than once.
            entity.HasIndex(payment => payment.OrderId)
                .IsUnique();

            entity.HasIndex(payment => payment.IdempotencyKey)
                .IsUnique();

            entity.Property(payment => payment.Amount)
                .HasPrecision(18, 2);

            entity.Property(payment => payment.Currency)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(payment => payment.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(payment => payment.Provider)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(payment => payment.ProviderTransactionId)
                .HasMaxLength(200);

            entity.Property(payment => payment.IdempotencyKey)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(payment => payment.FailureReason)
                .HasMaxLength(500);

            entity.Property(payment => payment.RowVersion)
                .IsRowVersion();
        });
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.MessageId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(message => message.CorrelationId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(message => message.CausationId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(message => message.MessageType)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(message => message.SourceService)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(message => message.Destination)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(message => message.Payload)
                .IsRequired();

            entity.Property(message => message.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(message => message.ErrorMessage)
                .HasMaxLength(4000);

            entity.Property(message => message.RowVersion)
                .IsRowVersion();

            entity.HasIndex(message => message.MessageId)
                .IsUnique();

            entity.HasIndex(message => new { message.Status, message.NextRetryAtUtc, message.CreatedAtUtc });
            entity.HasIndex(message => new { message.Status, message.ProcessingStartedAtUtc });
            entity.HasIndex(message => message.CorrelationId);
        });
    }

    private static void ConfigureInbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.MessageId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(message => message.CorrelationId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(message => message.CausationId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(message => message.MessageType)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(message => message.ConsumerName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(message => message.Payload)
                .IsRequired();

            entity.Property(message => message.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(message => message.ErrorMessage)
                .HasMaxLength(4000);

            entity.Property(message => message.RowVersion)
                .IsRowVersion();

            entity.HasIndex(message => new { message.MessageId, message.ConsumerName })
                .IsUnique();

            entity.HasIndex(message => new { message.Status, message.NextRetryAtUtc, message.ReceivedAtUtc });
            entity.HasIndex(message => message.CorrelationId);
        });
    }
}