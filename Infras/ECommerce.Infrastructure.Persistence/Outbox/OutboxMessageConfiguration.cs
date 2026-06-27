using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

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

        builder.Property(message => message.SourceService)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(message => message.Destination)
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
        // MessageId uniqueness prevents duplicate integration records in this store.
        // Broker delivery can still be duplicated, so consumers must remain idempotent.
        builder.HasIndex(message => message.MessageId)
            .IsUnique();

        builder.HasIndex(message => new
        {
            message.Status,
            message.NextRetryAtUtc,
            message.CreatedAtUtc
        });

        builder.HasIndex(message => message.CorrelationId);
    }
}
