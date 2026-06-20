
using ECommerce.Cart.Domain.Contracts.Catalog;
using ECommerce.Cart.Infrastructure.Client;
using ECommerce.Cart.Infrastructure.Clients;

using ECommerce.Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECommerce.Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCartInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddProductSnapshotClient(configuration);

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CartConnection")
            ?? configuration.GetConnectionString("ECommerceConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Cart database connection string is not configured.");
        }

        services.AddDbContext<CartDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<DbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<CartDbContext>());

        return services;
    }

    private static IServiceCollection AddProductSnapshotClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CatalogClientOptions>(
            configuration.GetSection(CatalogClientOptions.SectionName));

        services.AddHttpClient<IProductSnapshotClient, HttpProductSnapshotClient>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<CatalogClientOptions>>()
                    .Value;

                if (string.IsNullOrWhiteSpace(options.BaseAddress))
                {
                    throw new InvalidOperationException("CatalogClient:BaseAddress is not configured.");
                }

                client.BaseAddress = new Uri(options.BaseAddress);
            });

        return services;
    }
}