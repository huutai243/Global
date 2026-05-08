using ECommerce.Catalog.Core.Models;
using ECommerce.Catalog.Core.Requests;
using ECommerce.Catalog.Core.Responses;
using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Core.SharedLibs.Responses;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Inventory.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Catalog.Service.Features;

public sealed record GetPublicProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    bool Descending,
    int PageNumber,
    int PageSize) : IRequest<PagedResponse<ProductResponse>>;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse>;

public sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int InitialStock) : IRequest<ProductResponse>;

public sealed record UpdateProductCommand(
    Guid ProductId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    ProductStatus Status) : IRequest<ProductResponse>;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Price).GreaterThan(0);
        RuleFor(command => command.InitialStock).GreaterThanOrEqualTo(0);
    }
}

public sealed class GetPublicProductsQueryHandler(ECommerceDbContext dbContext)
    : IRequestHandler<GetPublicProductsQuery, PagedResponse<ProductResponse>>
{
    public async Task<PagedResponse<ProductResponse>> Handle(GetPublicProductsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var productsQuery = dbContext.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            productsQuery = productsQuery.Where(product => product.Name.Contains(request.SearchTerm));
        }

        if (request.CategoryId.HasValue)
        {
            productsQuery = productsQuery.Where(product => product.CategoryId == request.CategoryId);
        }

        if (request.MinPrice.HasValue)
        {
            productsQuery = productsQuery.Where(product => product.Price >= request.MinPrice);
        }

        if (request.MaxPrice.HasValue)
        {
            productsQuery = productsQuery.Where(product => product.Price <= request.MaxPrice);
        }

        productsQuery = (request.SortBy ?? string.Empty).ToLowerInvariant() switch
        {
            "name" => request.Descending ? productsQuery.OrderByDescending(product => product.Name) : productsQuery.OrderBy(product => product.Name),
            "price" => request.Descending ? productsQuery.OrderByDescending(product => product.Price) : productsQuery.OrderBy(product => product.Price),
            _ => request.Descending ? productsQuery.OrderByDescending(product => product.CreatedAt) : productsQuery.OrderBy(product => product.CreatedAt)
        };

        var totalCount = await productsQuery.CountAsync(cancellationToken);
        var products = await productsQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductResponse
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Status = product.Status.ToString()
            })
            .ToArrayAsync(cancellationToken);

        return new PagedResponse<ProductResponse>
        {
            Items = products,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

public sealed class GetProductByIdQueryHandler(
    ECommerceDbContext dbContext,
    IProductCache productCache,
    ILogger<GetProductByIdQueryHandler> logger)
    : IRequestHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<ProductResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"product:{request.ProductId}";

        try
        {
            var cachedProduct = await productCache.GetAsync<ProductResponse>(cacheKey, cancellationToken);
            if (cachedProduct is not null)
            {
                return cachedProduct;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis product cache read failed for {ProductId}", request.ProductId);
        }

        var product = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == request.ProductId && item.Status == ProductStatus.Active)
            .Select(item => new ProductResponse
            {
                Id = item.Id,
                CategoryId = item.CategoryId,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Status = item.Status.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        try
        {
            await productCache.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Redis product cache write failed for {ProductId}", request.ProductId);
        }

        return product;
    }
}

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

public sealed class UpdateProductCommandHandler(ECommerceDbContext dbContext, IProductCache productCache)
    : IRequestHandler<UpdateProductCommand, ProductResponse>
{
    public async Task<ProductResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        var categoryExists = await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new BusinessRuleException("Product must belong to an existing category.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.Price = request.Price;
        product.Status = request.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync($"product:{product.Id}", cancellationToken);

        return CatalogMapping.MapProduct(product);
    }
}

public sealed class DeleteProductCommandHandler(ECommerceDbContext dbContext, IProductCache productCache)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        product.Status = ProductStatus.Inactive;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await productCache.RemoveAsync($"product:{product.Id}", cancellationToken);
    }
}

public static class CatalogMapping
{
    public static ProductResponse MapProduct(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Status = product.Status.ToString()
        };
    }
}
