using ECommerce.Ordering.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentEntity = ECommerce.Payment.Domain.Models.Payment;

namespace ECommerce.Infrastructure.Persistence.Configurations.Payment;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> entity)
    {
        entity.HasKey(payment => payment.Id);

        entity.Property(payment => payment.Amount)
            .HasPrecision(18, 2);

        entity.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.Property(payment => payment.Provider)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(payment => payment.ProviderTransactionId)
            .HasMaxLength(200);

        entity.HasOne<Order>()
            .WithMany()
            .HasForeignKey(payment => payment.OrderId);
    }
}