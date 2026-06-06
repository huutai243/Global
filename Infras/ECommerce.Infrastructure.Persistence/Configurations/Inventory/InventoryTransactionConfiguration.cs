using ECommerce.Catalog.Domain.Models;
using ECommerce.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> entity)
    {
        entity.HasKey(transaction => transaction.Id);

        entity.Property(transaction => transaction.Reason)
            .HasMaxLength(500)
            .IsRequired();

        entity.HasOne<Product>()
            .WithMany()
            .HasForeignKey(transaction => transaction.ProductId);
    }
}