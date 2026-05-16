using ECommerce.Core.SharedLibs.Responses;
using ECommerce.Domain.Core.Catalog.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.GetPublicProducts;

public sealed record GetPublicProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    bool Descending,
    int PageNumber,
    int PageSize) : IRequest<PagedResponse<ProductResponse>>;
