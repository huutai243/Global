using ECommerce.Domain.Core.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Identity;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.HasKey(user => user.Id);

        entity.HasIndex(user => user.Email)
            .IsUnique();

        entity.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        entity.Property(user => user.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(user => user.Role)
            .HasMaxLength(50)
            .IsRequired();

        entity.HasOne(user => user.Customer)
            .WithOne(customer => customer.User)
            .HasForeignKey<Customer>(customer => customer.UserId);
    }
}