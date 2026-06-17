using ECommerce.Infrastructure.Security.Core;
using ECommerce.Ordering.Application.CheckoutCart;
using ECommerce.Ordering.Infrastructure;
using ECommerce.Shared.Core.Behaviors;
using ECommerce.Shared.Core.Identity;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
using ECommerce.Shared.Observability;
using ECommerce.Shared.Outbox;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ECommerce.Ordering.WebApi.Extensions;

public static class ServiceExtensions
{
    private const string AdminRole = "Admin";
    private const string CustomerRole = "Customer";

    public static IServiceCollection AddOrderingWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddCrossCuttingServices()
            .AddCurrentUserServices()
            .AddMessagingServices()
            .AddApplicationServices()
            .AddOrderingAuthentication(configuration)
            .AddOrderingAuthorization()
            .AddPresentationServices();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOrderingInfrastructure(configuration);
        services.AddCartCheckoutClient(configuration);

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

    private static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        services.AddSingleton<IMessageNameResolver, DefaultMessageNameResolver>();
        services.AddSingleton<OutboxMessageFactory>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddOrderingMediatR();
        services.AddOrderingValidation();

        return services;
    }

    private static IServiceCollection AddOrderingMediatR(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<CheckoutCartCommand>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddOrderingValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CheckoutCartCommand>, CheckoutCartCommandValidator>();

        return services;
    }

    private static IServiceCollection AddOrderingAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettingsSection = configuration.GetSection(nameof(JwtSettings));
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>() ?? new JwtSettings();

        services.Configure<JwtSettings>(jwtSettingsSection);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = CreateTokenValidationParameters(jwtSettings);
            });

        return services;
    }

    private static IServiceCollection AddOrderingAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminRole, policy => policy.RequireRole(AdminRole));
            options.AddPolicy(CustomerRole, policy => policy.RequireRole(CustomerRole, AdminRole));
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