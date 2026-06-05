using ECommerce.Domain.Core.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(token => token.CreatedAt)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.HasIndex(token => token.CustomerId);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasOne(token => token.Customer)
            .WithMany()
            .HasForeignKey(token => token.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}