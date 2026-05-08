using ECommerce.Core.SharedLibs.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Redis;

public static class RedisExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(nameof(RedisSettings)).Get<RedisSettings>() ?? new RedisSettings();
        services.Configure<RedisSettings>(configuration.GetSection(nameof(RedisSettings)));
        services.AddStackExchangeRedisCache(options => options.Configuration = settings.ConnectionString);
        services.AddScoped<IProductCache, RedisProductCache>();
        return services;
    }
}
