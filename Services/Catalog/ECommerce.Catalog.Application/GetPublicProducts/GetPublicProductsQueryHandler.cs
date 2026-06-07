using ECommerce.Shared.Core.Responses;
using ECommerce.Catalog.Domain.Models;
using ECommerce.Catalog.Domain.Responses;
using ECommerce.Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.GetPublicProducts;

public sealed class GetPublicProductsQueryHandler(CatalogDbContext dbContext)
    : IRequestHandler<GetPublicProductsQuery, PagedResponse<ProductResponse>>
{
    public async Task<PagedResponse<ProductResponse>> Handle(GetPublicProductsQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var productsQuery = dbContext.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Active && product.Category != null && product.Category.IsActive);

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
                CategoryName = product.Category!.Name,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
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
