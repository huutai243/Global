using ECommerce.Shared.Core.Responses;
using ECommerce.Catalog.Domain.Responses;
using MediatR;

namespace ECommerce.Catalog.Application.GetPublicProducts;

public sealed record GetPublicProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    bool Descending,
    int PageNumber,
    int PageSize) : IRequest<PagedResponse<ProductResponse>>;
