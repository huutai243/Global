using ECommerce.Catalog.Domain.Responses;

namespace ECommerce.Catalog.Application.Home;

public sealed class HomeResponse
{
    public IReadOnlyCollection<CategoryResponse> FeaturedCategories { get; init; } = [];

    public IReadOnlyCollection<ProductResponse> FeaturedProducts { get; init; } = [];

    public IReadOnlyCollection<ProductResponse> LatestProducts { get; init; } = [];
}
