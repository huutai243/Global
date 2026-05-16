using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Domain.Core.Inventory.Models;
using ECommerce.Domain.Service.Catalog.Mapping;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Catalog.CreateProduct;

public sealed class CreateProductCommandHandler(ECommerceDbContext dbContext, IProductCache productCache)
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await dbContext.Categories
            .AnyAsync(category => category.Id == request.CategoryId && category.IsActive, cancellationToken);

        if (!categoryExists)
        {
            throw new BusinessRuleException("Product must belong to an active category.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Products.Add(product);
        dbContext.InventoryItems.Add(new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            AvailableQuantity = request.InitialStock,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync($"product:{product.Id}", cancellationToken);

        return CatalogMapping.MapProduct(product);
    }
}
