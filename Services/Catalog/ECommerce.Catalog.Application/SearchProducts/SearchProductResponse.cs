namespace ECommerce.Catalog.Application.SearchProducts;

public sealed record SearchProductsResponse(
    IReadOnlyList<SearchProductItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static SearchProductsResponse Create(
        IReadOnlyList<SearchProductItemResponse> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new SearchProductsResponse(
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            totalPages > 0 && pageNumber < totalPages);
    }
}

public sealed record SearchProductItemResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal Price,
    string? ImageUrl,
    DateTime CreatedAt);