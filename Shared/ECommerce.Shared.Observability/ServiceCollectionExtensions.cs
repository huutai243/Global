using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        return services;
    }
}