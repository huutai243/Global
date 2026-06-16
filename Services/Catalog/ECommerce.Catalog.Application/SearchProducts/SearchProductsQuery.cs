using MediatR;

namespace ECommerce.Catalog.Application.SearchProducts;

public sealed record SearchProductsQuery : IRequest<SearchProductsResponse>
{
    public string? Keyword { get; init; }

    public Guid? CategoryId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public ProductSearchSort Sort { get; init; } = ProductSearchSort.Relevance;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}