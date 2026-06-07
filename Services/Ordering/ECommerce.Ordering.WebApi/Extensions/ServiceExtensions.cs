using ECommerce.Infrastructure.Observability;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Ordering.Application.CheckoutCart;
using ECommerce.Ordering.Infrastructure;
using ECommerce.Shared.Core.Behaviors;
using ECommerce.Shared.Core.Identity;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Messaging;
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
        services.AddOrderingInfrastructure(configuration);
        services.AddCartCheckoutClient(configuration);
        services.AddObservability();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IMessageNameResolver, DefaultMessageNameResolver>();

        services.AddOrderingMediatRServices();
        services.AddOrderingValidationServices();
        services.AddOrderingAuthenticationServices(configuration);
        services.AddOrderingAuthorizationServices();

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddOrderingMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<CheckoutCartCommand>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddOrderingValidationServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CheckoutCartCommand>, CheckoutCartCommandValidator>();

        return services;
    }

    private static IServiceCollection AddOrderingAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(nameof(JwtSettings))
            .Get<JwtSettings>() ?? new JwtSettings();

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

    private static IServiceCollection AddOrderingAuthorizationServices(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminRole, policy => policy.RequireRole(AdminRole));
            options.AddPolicy(CustomerRole, policy => policy.RequireRole(CustomerRole, AdminRole));
        });

        return services;
    }
}
