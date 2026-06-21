using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.RabbitMq.Configuration;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        services.AddSingleton<IMessageNameResolver, DefaultMessageNameResolver>();
        services.AddSingleton<RabbitMqPublisher>();
        services.AddSingleton<IMessagePublisher>(provider =>provider.GetRequiredService<RabbitMqPublisher>());
        services.AddSingleton<IRabbitMqPublisher>(provider =>provider.GetRequiredService<RabbitMqPublisher>());

        return services;
    }
}