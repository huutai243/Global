using ECommerce.Infrastructure.RabbitMq;
using ECommerce.Inventory.Application.ReserveInventory;
using ECommerce.Inventory.Infrastructure;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Observability;

namespace ECommerce.Inventory.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInventoryWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInventoryInfrastructure(configuration);
        services.AddRabbitMqMessaging(configuration);
        services.AddObservability();

        services.Configure<ReserveInventoryConsumerOptions>(
            configuration.GetSection(ReserveInventoryConsumerOptions.SectionName));

        services.AddScoped<ReserveInventoryCommandHandler>();

        services.AddHostedService<ReserveInventoryConsumer>();

        return services;
    }
}