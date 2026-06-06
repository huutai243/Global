using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services)
    {
        // TODO Phase 3: Move inventory-specific persistence, outbox, inbox, and messaging registrations here.
        return services;
    }
}
