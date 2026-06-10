using ECommerce.Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> entity)
    {
        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.ProductId).IsUnique();

        entity.Property(x => x.RowVersion).IsRowVersion();
    }
}