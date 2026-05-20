using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Catalog.GetProductById;

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
            .Where(item => item.Id == request.ProductId && item.Status == ProductStatus.Active && item.Category != null && item.Category.IsActive)
            .Select(item => new ProductResponse
            {
                Id = item.Id,
                CategoryId = item.CategoryId,
                CategoryName = item.Category!.Name,
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
