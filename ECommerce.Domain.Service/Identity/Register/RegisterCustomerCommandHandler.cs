using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Domain.Core.Identity.Models;
using ECommerce.Domain.Service.Identity.Shared;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Security.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Identity.Register;

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
            PasswordHash = PasswordHashing.HashPassword(request.Password),
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
}
