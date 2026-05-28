using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Home.GetHome;

public sealed class GetHomeQueryHandler(
    ECommerceDbContext dbContext,
    ILogger<GetHomeQueryHandler> logger)
    : IRequestHandler<GetHomeQuery, HomeResponse>
{
    private const int FeaturedCategoryLimit = 6;
    private const int FeaturedProductLimit = 8;
    private const int LatestProductLimit = 8;

    public async Task<HomeResponse> Handle(GetHomeQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting home data.");

        var featuredCategories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderByDescending(category => category.CreatedAt)
            .Take(FeaturedCategoryLimit)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            })
            .ToListAsync(cancellationToken);

        var featuredProducts = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Active)
            .OrderBy(product => product.Name)
            .Take(FeaturedProductLimit)
            .Select(product => new ProductResponse
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl
            })
            .ToListAsync(cancellationToken);

        var latestProducts = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Status == ProductStatus.Active)
            .OrderByDescending(product => product.CreatedAt)
            .Take(LatestProductLimit)
            .Select(product => new ProductResponse
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl
            })
            .ToListAsync(cancellationToken);

        return new HomeResponse
        {
            FeaturedCategories = featuredCategories,
            FeaturedProducts = featuredProducts,
            LatestProducts = latestProducts
        };
    }
}