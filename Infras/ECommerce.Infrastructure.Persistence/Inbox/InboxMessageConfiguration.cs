using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Inbox;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.MessageId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(message => message.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(message => message.CausationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(message => message.MessageType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(message => message.ConsumerName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(message => message.Payload)
            .IsRequired();

        builder.Property(message => message.Status)
            .IsRequired();

        builder.Property(message => message.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(message => message.RowVersion)
            .IsRowVersion();

        // IDEMPOTENCY NOTE:
        // MessageId + ConsumerName is the durable duplicate-detection key for at-least-once delivery.
        builder.HasIndex(message => new
        {
            message.MessageId,
            message.ConsumerName
        }).IsUnique();

        builder.HasIndex(message => new
        {
            message.Status,
            message.NextRetryAtUtc,
            message.ReceivedAtUtc
        });

        builder.HasIndex(message => message.CorrelationId);
    }
}
