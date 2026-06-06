using ECommerce.Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.HasKey(product => product.Id);

        entity.Property(product => product.Name)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(product => product.Description)
            .HasMaxLength(2000);

        entity.Property(product => product.Price)
            .HasPrecision(18, 2);

        entity.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity.HasIndex(product => product.CategoryId);
    }
}