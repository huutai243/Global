using System.Text;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Payment.Infrastructure;
using ECommerce.Shared.Core.Identity;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Payment.WebApi.Extensions;

public static class ServiceExtensions
{
    private const string AdminRole = "Admin";
    private const string CustomerRole = "Customer";

    public static IServiceCollection AddPaymentWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddCrossCuttingServices()
            .AddCurrentUserServices()
            .AddPaymentInfrastructure(configuration)
            .AddPaymentAuthentication(configuration)
            .AddPaymentAuthorization()
            .AddPresentationServices();

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

    private static IServiceCollection AddPaymentAuthentication(
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

    private static IServiceCollection AddPaymentAuthorization(this IServiceCollection services)
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