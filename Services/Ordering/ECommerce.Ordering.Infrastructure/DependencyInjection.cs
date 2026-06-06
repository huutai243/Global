using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services)
    {
        // TODO Phase 3: Move ordering-specific persistence, outbox, inbox, and messaging registrations here.
        return services;
    }
}
