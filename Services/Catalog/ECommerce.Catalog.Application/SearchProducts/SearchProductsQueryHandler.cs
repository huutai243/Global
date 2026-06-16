using ECommerce.Catalog.Domain.Models;
using ECommerce.Catalog.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Application.SearchProducts;

public sealed class SearchProductsQueryHandler(
    CatalogDbContext dbContext)
    : IRequestHandler<SearchProductsQuery, SearchProductsResponse>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<SearchProductsResponse> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var keyword = NormalizeKeyword(request.Keyword);

        var query = dbContext.Products
            .AsNoTracking()
            .AsQueryable();

        query = ApplyPublicProductFilter(query);
        query = ApplyKeywordFilter(query, keyword);
        query = ApplyCategoryFilter(query, request.CategoryId);
        query = ApplyPriceFilter(query, request.MinPrice, request.MaxPrice);
        query = ApplySorting(query, request.Sort, keyword);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new SearchProductItemResponse(
                product.Id,
                product.CategoryId,
                product.Category == null ? string.Empty : product.Category.Name,
                product.Name,
                product.Description,
                product.Price,
                product.ImageUrl,
                product.CreatedAt))
            .ToListAsync(cancellationToken);

        return SearchProductsResponse.Create(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    private static IQueryable<Product> ApplyPublicProductFilter(
        IQueryable<Product> query)
    {
        return query.Where(product => product.Status == ProductStatus.Active);
    }

    private static IQueryable<Product> ApplyKeywordFilter(
        IQueryable<Product> query,
        string? keyword)
    {
        if (keyword is null)
        {
            return query;
        }

        var containsPattern = CreateContainsPattern(keyword);

        return query.Where(product =>
            EF.Functions.Like(product.Name, containsPattern, LikeEscapeCharacter) ||
            product.Description != null &&
            EF.Functions.Like(product.Description, containsPattern, LikeEscapeCharacter) ||
            product.Category != null &&
            EF.Functions.Like(product.Category.Name, containsPattern, LikeEscapeCharacter));
    }

    private static IQueryable<Product> ApplyCategoryFilter(
        IQueryable<Product> query,
        Guid? categoryId)
    {
        if (categoryId is null || categoryId == Guid.Empty)
        {
            return query;
        }

        return query.Where(product => product.CategoryId == categoryId);
    }

    private static IQueryable<Product> ApplyPriceFilter(
        IQueryable<Product> query,
        decimal? minPrice,
        decimal? maxPrice)
    {
        if (minPrice is not null)
        {
            query = query.Where(product => product.Price >= minPrice);
        }

        if (maxPrice is not null)
        {
            query = query.Where(product => product.Price <= maxPrice);
        }

        return query;
    }

    private static IQueryable<Product> ApplySorting(
        IQueryable<Product> query,
        ProductSearchSort sort,
        string? keyword)
    {
        if (sort == ProductSearchSort.Relevance && keyword is not null)
        {
            return query
                .OrderByDescending(product => product.Name == keyword)
                .ThenByDescending(product => product.Name.StartsWith(keyword))
                .ThenByDescending(product => product.CreatedAt)
                .ThenBy(product => product.Name);
        }

        return sort switch
        {
            ProductSearchSort.PriceLowToHigh => query
                .OrderBy(product => product.Price)
                .ThenBy(product => product.Name),

            ProductSearchSort.PriceHighToLow => query
                .OrderByDescending(product => product.Price)
                .ThenBy(product => product.Name),

            ProductSearchSort.NameAscending => query
                .OrderBy(product => product.Name)
                .ThenByDescending(product => product.CreatedAt),

            ProductSearchSort.NameDescending => query
                .OrderByDescending(product => product.Name)
                .ThenByDescending(product => product.CreatedAt),

            ProductSearchSort.Newest => query
                .OrderByDescending(product => product.CreatedAt)
                .ThenBy(product => product.Name),

            _ => query
                .OrderByDescending(product => product.CreatedAt)
                .ThenBy(product => product.Name)
        };
    }

    private static string? NormalizeKeyword(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return null;
        }

        return keyword.Trim();
    }

    private static string CreateContainsPattern(string keyword)
    {
        return $"%{EscapeLikePattern(keyword)}%";
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
    }
}