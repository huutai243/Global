
using ECommerce.Identity.Application.ForgotPassword;
using ECommerce.Identity.Application.Login;
using ECommerce.Identity.Application.Register;
using ECommerce.Identity.Application.ResetPassword;
using ECommerce.Identity.Domain.Models;
using ECommerce.Shared.Observability;
using ECommerce.Identity.Infrastructure;
using ECommerce.Infrastructure.Security;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Shared.Core.Behaviors;
using ECommerce.Shared.Core.Identity;
using ECommerce.Shared.Core.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ECommerce.Identity.WebApi.Infras.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddIdentityApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityInfrastructure(configuration);
        services.AddObservability();

        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        services.AddIdentityMediatRServices();
        services.AddIdentityValidationServices();
        services.AddIdentityAuthenticationServices(configuration);
        services.AddIdentityAuthorizationServices();
        services.AddIdentityEmailServices(configuration);
        services.AddIdentityPasswordResetServices();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddIdentityMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<LoginCommand>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }

    private static IServiceCollection AddIdentityValidationServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterCustomerCommand>, RegisterCustomerCommandValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddScoped<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();

        return services;
    }

    private static IServiceCollection AddIdentityAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(nameof(JwtSettings))
            .Get<JwtSettings>() ?? new JwtSettings();

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

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

    private static IServiceCollection AddIdentityAuthorizationServices(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(UserRoles.Admin, policy => policy.RequireRole(UserRoles.Admin));
            options.AddPolicy(UserRoles.Customer, policy => policy.RequireRole(UserRoles.Customer, UserRoles.Admin));
        });

        return services;
    }

    private static IServiceCollection AddIdentityEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }

    private static IServiceCollection AddIdentityPasswordResetServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();

        return services;
    }
}
