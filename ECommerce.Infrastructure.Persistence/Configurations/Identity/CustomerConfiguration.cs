using ECommerce.Domain.Core.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Identity;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> entity)
    {
        entity.HasKey(customer => customer.Id);

        entity.Property(customer => customer.FullName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(customer => customer.Email)
            .HasMaxLength(320)
            .IsRequired();
    }
}