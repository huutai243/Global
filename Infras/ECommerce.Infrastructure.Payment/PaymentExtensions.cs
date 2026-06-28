using ECommerce.Payment.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Payment;

public static class PaymentExtensions
{
    public static IServiceCollection AddStripePaymentGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StripeOptions>(
            configuration.GetSection(StripeOptions.SectionName));

        services.AddScoped<IPaymentGateway, StripePaymentProvider>();

        return services;
    }
}