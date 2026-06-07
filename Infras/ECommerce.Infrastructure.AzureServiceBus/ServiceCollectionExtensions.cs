using Azure.Messaging.ServiceBus;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.AzureServiceBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureServiceBusOptions>(
            configuration.GetSection(AzureServiceBusOptions.SectionName));

        services.AddSingleton<IMessageNameResolver, DefaultMessageNameResolver>();

        services.AddSingleton(provider =>
        {
            var options = provider
                .GetRequiredService<IOptions<AzureServiceBusOptions>>()
                .Value;

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                throw new InvalidOperationException("Azure Service Bus connection string is not configured.");
            }

            // ServiceBusClient is thread-safe and should be reused instead of created per message.
            return new ServiceBusClient(options.ConnectionString);
        });

        services.AddScoped<IMessagePublisher, AzureServiceBusMessagePublisher>();

        return services;
    }
}