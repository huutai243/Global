using ECommerce.Domain.Service.Identity.Shared;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Security.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Identity.Login;

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

        if (user is null || user.PasswordHash != PasswordHashing.HashPassword(request.Password) || !user.IsActive)
        {
            logger.LogWarning("Login failure for email {Email}", normalizedEmail);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var token = jwtTokenService.CreateToken(user.Id, user.Customer?.Id, user.Email, user.Role);
        return new AuthResponse(user.Id, user.Customer?.Id, user.Email, user.Role, token);
    }
}
