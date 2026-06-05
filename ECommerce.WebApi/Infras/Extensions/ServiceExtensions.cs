using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Identity.Models;
using ECommerce.Domain.Service.Cart.AddCartItem;
using ECommerce.Domain.Service.Catalog.CreateCategory;
using ECommerce.Domain.Service.Catalog.CreateProduct;
using ECommerce.Domain.Service.Catalog.GetPublicProducts;
using ECommerce.Domain.Service.Catalog.UpdateCategory;
using ECommerce.Domain.Service.Catalog.UpdateProduct;
using ECommerce.Domain.Service.Identity.ForgotPassword;
using ECommerce.Domain.Service.Identity.Login;
using ECommerce.Domain.Service.Identity.Register;
using ECommerce.Domain.Service.Identity.ResetPassword;
using ECommerce.Domain.Service.Inventory.AdjustInventory;
using ECommerce.Domain.Service.Ordering.CheckoutCart;
using ECommerce.Domain.Service.Payment.PayOrder;
using ECommerce.Infrastructure.Kafka;
using ECommerce.Infrastructure.Observability;
using ECommerce.Infrastructure.Payment;
using ECommerce.Infrastructure.Persistence.Extensions;
using ECommerce.Infrastructure.RabbitMq;
using ECommerce.Infrastructure.Redis;
using ECommerce.Infrastructure.Security;
using ECommerce.Infrastructure.Security.Core;
using ECommerce.Infrastructure.Storage;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ECommerce.WebApi.Infras.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddBlobStorage(configuration);
        services.AddObservability();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddMediatRServices();
        services.AddValidationServices();
        services.AddAuthenticationServices(configuration);
        services.AddEmailServices(configuration);
        services.AddPasswordResetServices();
        services.AddAuthorizationServices();
        services.AddRedisCache(configuration);
        services.AddRabbitMqMessaging(configuration);
        services.AddKafkaMessaging(configuration);
        services.AddPaymentServices();
        services.AddSwaggerDocumentation();
        services.AddHealthCheckServices();

        return services;
    }

    public static IServiceCollection AddMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<GetPublicProductsQuery>();
            configuration.RegisterServicesFromAssemblyContaining<RegisterCustomerCommand>();
            configuration.RegisterServicesFromAssemblyContaining<AdjustInventoryCommand>();
            configuration.RegisterServicesFromAssemblyContaining<AddCartItemCommand>();
            configuration.RegisterServicesFromAssemblyContaining<PayOrderCommand>();
        });

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        return services;
    }

    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();
        services.AddScoped<IValidator<CreateCategoryCommand>, CreateCategoryCommandValidator>();
        services.AddScoped<IValidator<UpdateCategoryCommand>, UpdateCategoryCommandValidator>();
        services.AddScoped<IValidator<RegisterCustomerCommand>, RegisterCustomerCommandValidator>();
        services.AddScoped<IValidator<ForgotPasswordCommand>, ForgotPasswordCommandValidator>();
        services.AddScoped<IValidator<ResetPasswordCommand>, ResetPasswordCommandValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<AdjustInventoryCommand>, AdjustInventoryCommandValidator>();
        services.AddScoped<IValidator<AddCartItemCommand>, AddCartItemCommandValidator>();
        services.AddScoped<IValidator<CheckoutCartCommand>, CheckoutCartCommandValidator>();
        services.AddScoped<IValidator<PayOrderCommand>, PayOrderCommandValidator>();
        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>() ?? new JwtSettings();
        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };
            });

        return services;
    }

    public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(UserRoles.Admin, policy => policy.RequireRole(UserRoles.Admin));
            options.AddPolicy(UserRoles.Customer, policy => policy.RequireRole(UserRoles.Customer, UserRoles.Admin));
        });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }

    public static IServiceCollection AddPasswordResetServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();

        return services;
    }
}
