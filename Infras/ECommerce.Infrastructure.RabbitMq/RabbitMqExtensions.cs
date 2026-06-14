using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.RabbitMq;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        services.AddSingleton<IMessageNameResolver, DefaultMessageNameResolver>();

        return services;
    }
}