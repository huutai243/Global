using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Domain.Service.Identity.Shared;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Security.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Identity.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    ECommerceDbContext dbContext,
    IPasswordResetTokenService passwordResetTokenService)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new BusinessRuleException("Password confirmation does not match.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.ApplicationUsers
            .FirstOrDefaultAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleException("Invalid reset token.");
        }

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(
                customer => customer.UserId == user.Id,
                cancellationToken);

        if (customer is null)
        {
            throw new BusinessRuleException("Invalid reset token.");
        }

        var tokenHash = passwordResetTokenService.HashToken(request.Token);

        var passwordResetToken = await dbContext.PasswordResetTokens
            .FirstOrDefaultAsync(
                token =>
                    token.CustomerId == customer.Id &&
                    token.TokenHash == tokenHash &&
                    token.UsedAt == null,
                cancellationToken);

        if (passwordResetToken is null)
        {
            throw new BusinessRuleException("Invalid reset token.");
        }

        if (passwordResetToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new BusinessRuleException("Reset token has expired.");
        }

        user.PasswordHash = PasswordHashing.HashPassword(request.NewPassword);
        passwordResetToken.UsedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}