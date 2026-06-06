using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        // TODO Phase 3: Move identity-specific security, persistence, outbox, inbox, and messaging registrations here.
        return services;
    }
}
