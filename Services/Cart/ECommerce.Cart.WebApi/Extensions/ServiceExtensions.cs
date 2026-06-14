using System.Text;
using ECommerce.Cart.Application.AddCartItem;
using ECommerce.Cart.Infrastructure;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Shared.Core.Behaviors;
using ECommerce.Shared.Core.Identity;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Observability;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Cart.WebApi.Extensions;

public static class ServiceExtensions
{
    private const string FrontendCorsPolicy = "Frontend";
    private const string FrontendBaseUrlKey = "Frontend:BaseUrl";

    public static IServiceCollection AddCartApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddCrossCuttingServices()
            .AddCurrentUserServices()
            .AddApplicationServices()
            .AddCartAuthentication(configuration)
            .AddCartAuthorization()
            .AddCorsServices(configuration)
            .AddPresentationServices();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCartInfrastructure(configuration);

        return services;
    }

    private static IServiceCollection AddCrossCuttingServices(this IServiceCollection services)
    {
        services.AddObservability();

        return services;
    }

    private static IServiceCollection AddCurrentUserServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddCartMediatR();
        services.AddCartValidation();

        return services;
    }

    private static IServiceCollection AddCartMediatR(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<AddCartItemCommand>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddCartValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<AddCartItemCommand>, AddCartItemCommandValidator>();

        return services;
    }

    private static IServiceCollection AddCartAuthentication(
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

    private static IServiceCollection AddCartAuthorization(this IServiceCollection services)
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