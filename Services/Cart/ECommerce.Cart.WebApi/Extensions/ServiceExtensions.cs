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
using System.Text;

namespace ECommerce.Cart.WebApi.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCartApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCartInfrastructure(configuration);
        services.AddObservability();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        services.AddCartMediatRServices();
        services.AddCartValidationServices();
        services.AddCartAuthenticationServices(configuration);
        services.AddAuthorization();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddCartMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<AddCartItemCommand>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddCartValidationServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<AddCartItemCommand>, AddCartItemCommandValidator>();

        return services;
    }

    private static IServiceCollection AddCartAuthenticationServices(
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
}
