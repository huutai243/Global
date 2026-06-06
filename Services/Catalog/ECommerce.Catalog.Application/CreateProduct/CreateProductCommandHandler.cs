using ECommerce.Shared.Core.Exceptions;
using ECommerce.Catalog.Domain.Models;
using ECommerce.Catalog.Domain.Responses;
using ECommerce.Inventory.Domain.Models;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.CreateProduct;

public sealed class CreateProductCommandHandler(
    ECommerceDbContext dbContext,
    IBlobStorageService blobStorageService)
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.CategoryId && item.IsActive,
                cancellationToken);

        if (category is null)
        {
            throw new BusinessRuleException("Product must belong to an active category.");
        }

        string? imageUrl = null;

        if (request.Image is not null)
        {
            var uploadRequest = new FileUploadRequest
            {
                Content = request.Image.Content,
                FileName = request.Image.FileName,
                ContentType = request.Image.ContentType,
                FolderPath = "products"
            };

            var uploadResult = await blobStorageService.UploadAsync(
                uploadRequest,
                cancellationToken);

            imageUrl = uploadResult.Url;
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            ImageUrl = imageUrl,
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

        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            Status = product.Status.ToString()
        };
    }
}