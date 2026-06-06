using ECommerce.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Integration;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> entity)
    {
        entity.HasKey(idempotencyRecord => idempotencyRecord.Id);

        entity.HasIndex(idempotencyRecord => idempotencyRecord.Key)
            .IsUnique();

        entity.Property(idempotencyRecord => idempotencyRecord.Key)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(idempotencyRecord => idempotencyRecord.RequestHash)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(idempotencyRecord => idempotencyRecord.ResponsePayload)
            .HasMaxLength(4000);

        entity.Property(idempotencyRecord => idempotencyRecord.Status)
            .HasMaxLength(50)
            .IsRequired();
    }
}