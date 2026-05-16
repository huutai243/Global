using ECommerce.Domain.Core.Payment.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Payment;

public static class PaymentExtensions
{
    public static IServiceCollection AddPaymentServices(this IServiceCollection services)
    {
        services.AddScoped<IPaymentProvider, FakePaymentProvider>();
        return services;
    }
}
