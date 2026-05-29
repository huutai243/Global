using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Inventory.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations.Inventory;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> entity)
    {
        entity.HasKey(inventoryItem => inventoryItem.Id);

        entity.HasIndex(inventoryItem => inventoryItem.ProductId)
            .IsUnique();

        entity.Property(inventoryItem => inventoryItem.RowVersion)
            .IsRowVersion();

        entity.HasOne<Product>()
            .WithOne()
            .HasForeignKey<InventoryItem>(inventoryItem => inventoryItem.ProductId);
    }
}