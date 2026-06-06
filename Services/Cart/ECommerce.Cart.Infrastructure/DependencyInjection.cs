using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCartInfrastructure(this IServiceCollection services)
    {
        // TODO Phase 3: Move cart-specific persistence, outbox, inbox, and messaging registrations here.
        return services;
    }
}
