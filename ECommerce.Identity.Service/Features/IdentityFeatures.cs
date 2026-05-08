using System.Security.Cryptography;
using System.Text;
using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Identity.Core.Models;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Security.Core;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Identity.Service.Features;

public sealed record RegisterCustomerCommand(string Email, string Password, string FullName) : IRequest<AuthResponse>;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed record AuthResponse(Guid UserId, Guid? CustomerId, string Email, string Role, string AccessToken);

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).MinimumLength(8);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}

public sealed class RegisterCustomerCommandHandler(
    ECommerceDbContext dbContext,
    IJwtTokenService jwtTokenService,
    ILogger<RegisterCustomerCommandHandler> logger)
    : IRequestHandler<RegisterCustomerCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.ApplicationUsers.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new BusinessRuleException("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = UserRoles.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.ApplicationUsers.Add(user);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Register success for user {UserId}", user.Id);
        var token = jwtTokenService.CreateToken(user.Id, customer.Id, user.Email, user.Role);
        return new AuthResponse(user.Id, customer.Id, user.Email, user.Role, token);
    }

    internal static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

public sealed class LoginCommandHandler(
    ECommerceDbContext dbContext,
    IJwtTokenService jwtTokenService,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.ApplicationUsers
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);

        if (user is null || user.PasswordHash != RegisterCustomerCommandHandler.HashPassword(request.Password) || !user.IsActive)
        {
            logger.LogWarning("Login failure for email {Email}", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var token = jwtTokenService.CreateToken(user.Id, user.Customer?.Id, user.Email, user.Role);
        return new AuthResponse(user.Id, user.Customer?.Id, user.Email, user.Role, token);
    }
}
