using ECommerce.Core.SharedLibs.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure.Redis;

public static class RedisExtensions
{
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSection = configuration.GetSection(nameof(RedisSettings));
        var redisSettings = redisSection.Get<RedisSettings>() ?? new RedisSettings();

        services.Configure<RedisSettings>(redisSection);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisSettings.ConnectionString;
        });

        services.AddScoped<IProductCache, RedisProductCache>();

        return services;
    }
}