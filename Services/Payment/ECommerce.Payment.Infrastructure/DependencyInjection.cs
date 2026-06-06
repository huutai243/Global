using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services)
    {
        // TODO Phase 3: Move payment-specific provider, persistence, outbox, inbox, and messaging registrations here.
        return services;
    }
}
