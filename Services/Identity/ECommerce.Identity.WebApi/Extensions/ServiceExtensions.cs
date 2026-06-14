using System.Text;
using ECommerce.Identity.Application.ForgotPassword;
using ECommerce.Identity.Application.Login;
using ECommerce.Identity.Application.Register;
using ECommerce.Identity.Application.ResetPassword;
using ECommerce.Identity.Domain.Models;
using ECommerce.Identity.Infrastructure;
using ECommerce.Infrastructure.Security;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Shared.Core.Behaviors;
using ECommerce.Shared.Core.Identity;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Observability;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Identity.WebApi.Infras.Extensions;

public static class ServiceExtensions
{
    private const string FrontendCorsPolicy = "Frontend";
    private const string FrontendBaseUrlKey = "Frontend:BaseUrl";
    private const string SmtpSectionName = "Smtp";

    public static IServiceCollection AddIdentityApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddInfrastructure(configuration)
            .AddCrossCuttingServices()
            .AddCurrentUserServices()
            .AddApplicationServices()
            .AddIdentityAuthentication(configuration)
            .AddIdentityAuthorization()
            .AddEmailServices(configuration)
            .AddPasswordResetServices()
            .AddCorsServices(configuration)
            .AddPresentationServices();

        return services;
    }

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityInfrastructure(configuration);

        return services;
    }

    private static IServiceCollection AddCrossCuttingServices(this IServiceCollection services)
    {
        services.AddObservability();

        return services;
    }

    private static IServiceCollection AddCurrentUserServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddIdentityMediatR();
        services.AddIdentityValidation();

        return services;
    }

    private static IServiceCollection AddIdentityMediatR(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<LoginCommand>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddIdentityValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterCustomerCommand>, RegisterCustomerCommandValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddScoped<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();

        return services;
    }

    private static IServiceCollection AddIdentityAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettingsSection = configuration.GetSection(nameof(JwtSettings));
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>() ?? new JwtSettings();

        services.Configure<JwtSettings>(jwtSettingsSection);
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = CreateTokenValidationParameters(jwtSettings);
            });

        return services;
    }

    private static IServiceCollection AddIdentityAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(UserRoles.Admin, policy => policy.RequireRole(UserRoles.Admin));
            options.AddPolicy(UserRoles.Customer, policy => policy.RequireRole(UserRoles.Customer, UserRoles.Admin));
        });

        return services;
    }

    private static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }

    private static IServiceCollection AddPasswordResetServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();

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