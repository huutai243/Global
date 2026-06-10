using ECommerce.Infrastructure.Security.Core;
using ECommerce.Shared.Core.Identity;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ECommerce.Inventory.WebApi.Extensions;

public static class ServiceExtensions
{
    private const string AdminRole = "Admin";
    private const string CustomerRole = "Customer";

    public static IServiceCollection AddInventoryWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddObservability();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        services.AddInventoryAuthenticationServices(configuration);
        services.AddInventoryAuthorizationServices();

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddInventoryAuthenticationServices(
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

    private static IServiceCollection AddInventoryAuthorizationServices(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminRole, policy => policy.RequireRole(AdminRole));
            options.AddPolicy(CustomerRole, policy => policy.RequireRole(CustomerRole, AdminRole));
        });

        return services;
    }
}