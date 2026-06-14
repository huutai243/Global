using System.Text;
using ECommerce.Catalog.Application.CreateCategory;
using ECommerce.Catalog.Application.CreateProduct;
using ECommerce.Catalog.Application.GetPublicProducts;
using ECommerce.Catalog.Application.UpdateCategory;
using ECommerce.Catalog.Application.UpdateProduct;
using ECommerce.Catalog.Infrastructure;
using ECommerce.Infrastructure.Redis;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Infrastructure.Storage;
using ECommerce.Shared.Core.Behaviors;
using ECommerce.Shared.Core.Helpers;
using ECommerce.Shared.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Catalog.WebApi.Extensions;

public static class ServiceExtensions
{
    private const string FrontendCorsPolicy = "Frontend";
    private const string FrontendBaseUrlKey = "Frontend:BaseUrl";

    public static IServiceCollection AddCatalogApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddCrossCuttingServices()
            .AddApplicationServices()
            .AddCatalogAuthentication(configuration)
            .AddCatalogAuthorization()
            .AddCorsServices(configuration)
            .AddPresentationServices();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCatalogInfrastructure(configuration);
        services.AddBlobStorage(configuration);
        services.AddRedisCache(configuration);

        return services;
    }

    private static IServiceCollection AddCrossCuttingServices(this IServiceCollection services)
    {
        services.AddSingleton<IJsonHelper, JsonHelper>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddCatalogMediatR();
        services.AddCatalogValidation();

        return services;
    }

    private static IServiceCollection AddCatalogMediatR(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<GetPublicProductsQuery>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddCatalogValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();
        services.AddScoped<IValidator<CreateCategoryCommand>, CreateCategoryCommandValidator>();
        services.AddScoped<IValidator<UpdateCategoryCommand>, UpdateCategoryCommandValidator>();

        return services;
    }

    private static IServiceCollection AddCatalogAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettingsSection = configuration.GetSection(nameof(JwtSettings));
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>() ?? new JwtSettings();

        EnsureValidJwtSettings(jwtSettings);

        services.Configure<JwtSettings>(jwtSettingsSection);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = CreateTokenValidationParameters(jwtSettings);
            });

        return services;
    }

    private static IServiceCollection AddCatalogAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();

        return services;
    }

    private static IServiceCollection AddCorsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var frontendBaseUrl = configuration[FrontendBaseUrlKey];

        EnsureValidFrontendBaseUrl(frontendBaseUrl);

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy
                    .WithOrigins(frontendBaseUrl!)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    private static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();

        return services;
    }

    private static void EnsureValidJwtSettings(JwtSettings jwtSettings)
    {
        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JwtSettings:SecretKey is missing.");
        }
    }

    private static void EnsureValidFrontendBaseUrl(string? frontendBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new InvalidOperationException($"{FrontendBaseUrlKey} is missing.");
        }
    }

    private static TokenValidationParameters CreateTokenValidationParameters(JwtSettings jwtSettings)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = CreateIssuerSigningKey(jwtSettings)
        };
    }

    private static SymmetricSecurityKey CreateIssuerSigningKey(JwtSettings jwtSettings)
    {
        return new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
    }
}