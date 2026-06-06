using ECommerce.Catalog.Application.CreateCategory;
using ECommerce.Catalog.Application.CreateProduct;
using ECommerce.Catalog.Application.GetCategoryById;
using ECommerce.Catalog.Application.GetProductById;
using ECommerce.Catalog.Application.GetPublicProducts;
using ECommerce.Catalog.Application.UpdateCategory;
using ECommerce.Catalog.Application.UpdateProduct;
using ECommerce.Infrastructure.Persistence.Extensions;
using ECommerce.Infrastructure.Redis;
using ECommerce.Infrastructure.Security;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Infrastructure.Storage;
using ECommerce.Shared.Core.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ECommerce.Catalog.WebApi.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCatalogApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddBlobStorage(configuration);
        services.AddRedisCache(configuration);

        services.AddCatalogMediatRServices();
        services.AddCatalogValidationServices();
        services.AddCatalogAuthenticationServices(configuration);
        services.AddCatalogAuthorizationServices();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddCatalogMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<GetPublicProductsQuery>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddCatalogValidationServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();
        services.AddScoped<IValidator<CreateCategoryCommand>, CreateCategoryCommandValidator>();
        services.AddScoped<IValidator<UpdateCategoryCommand>, UpdateCategoryCommandValidator>();

        return services;
    }

    private static IServiceCollection AddCatalogAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(nameof(JwtSettings))
            .Get<JwtSettings>() ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JwtSettings:SecretKey is missing.");
        }

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };
            });

        return services;
    }

    private static IServiceCollection AddCatalogAuthorizationServices(this IServiceCollection services)
    {
        services.AddAuthorization();

        return services;
    }
}