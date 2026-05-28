using ECommerce.Domain.Core.Catalog.Responses;

namespace ECommerce.Domain.Service.Home.GetHome;

public sealed class HomeResponse
{
    public IReadOnlyCollection<CategoryResponse> FeaturedCategories { get; init; } = [];

    public IReadOnlyCollection<ProductResponse> FeaturedProducts { get; init; } = [];

    public IReadOnlyCollection<ProductResponse> LatestProducts { get; init; } = [];
}