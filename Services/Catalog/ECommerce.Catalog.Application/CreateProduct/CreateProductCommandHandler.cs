using ECommerce.Catalog.Domain.Models;
using ECommerce.Catalog.Domain.Responses;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Infrastructure.Storage;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.CreateProduct;

public sealed class CreateProductCommandHandler(
    CatalogDbContext dbContext,
    IBlobStorageService blobStorageService,
    OutboxMessageFactory outboxMessageFactory)
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private const string SourceService = "Catalog";
    private const string DestinationService = "Inventory";

    public async Task<ProductResponse> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
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

        var utcNow = DateTime.UtcNow;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            ImageUrl = imageUrl,
            Status = ProductStatus.Active,
            CreatedAt = utcNow
        };

        var productCreatedEvent = new ProductCreatedEvent(
            product.Id,
            product.Name,
            request.InitialStock,
            utcNow);

        var messageId = Guid.NewGuid().ToString("N");

        var outboxMessage = outboxMessageFactory.Create(
            productCreatedEvent,
            SourceService,
            DestinationService,
            messageId,
            messageId,
            utcNow);

        dbContext.Products.Add(product);
        dbContext.OutboxMessages.Add(outboxMessage);

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