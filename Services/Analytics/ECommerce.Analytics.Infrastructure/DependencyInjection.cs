using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services)
    {
        // TODO Phase 3: Add analytics producer/consumer registrations when analytics flows become service-specific.
        return services;
    }
}
