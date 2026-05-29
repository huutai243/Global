using ECommerce.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Integration;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> entity)
    {
        entity.HasKey(outboxMessage => outboxMessage.Id);

        entity.Property(outboxMessage => outboxMessage.EventType)
            .HasMaxLength(300)
            .IsRequired();

        entity.Property(outboxMessage => outboxMessage.Payload)
            .IsRequired();

        entity.Property(outboxMessage => outboxMessage.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.Property(outboxMessage => outboxMessage.ErrorMessage)
            .HasMaxLength(2000);

        entity.HasIndex(outboxMessage => new
        {
            outboxMessage.Status,
            outboxMessage.NextRetryAt
        });
    }
}