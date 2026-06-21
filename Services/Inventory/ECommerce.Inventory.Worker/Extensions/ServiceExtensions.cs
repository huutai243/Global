using ECommerce.Infrastructure.RabbitMq.Configuration;
using ECommerce.Inventory.Application.ProductCreated;
using ECommerce.Inventory.Application.ReserveInventory;
using ECommerce.Inventory.Infrastructure;
using ECommerce.Inventory.Worker.Consumers;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Core.Helpers;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Observability;
using ECommerce.Shared.Outbox;
using FluentValidation;

namespace ECommerce.Inventory.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInventoryWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddApplicationServices()
            .AddWorkerOptions(configuration)
            .AddBackgroundWorkers();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddInventoryInfrastructure(configuration);
        services.AddRabbitMqMessaging(configuration);
        services.AddObservability();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ReserveInventoryCommandValidator>();
        services.AddSingleton<IJsonHelper, JsonHelper>();

        services.AddScoped<ReserveInventoryCommandHandler>();
        services.AddScoped<ProductCreatedEventHandler>();
        services.AddScoped<OutboxMessageFactory>();

        return services;
    }

    private static IServiceCollection AddWorkerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ReserveInventoryConsumerOptions>(
            configuration.GetSection(ReserveInventoryConsumerOptions.SectionName));

        services.Configure<ProductCreatedConsumerOptions>(
            configuration.GetSection(ProductCreatedConsumerOptions.SectionName));

        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<ReserveInventoryConsumer>();
        services.AddHostedService<ProductCreatedConsumer>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}