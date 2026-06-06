using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        // TODO Phase 3: Move catalog-specific cache, storage, persistence, and messaging registrations here.
        return services;
    }
}
