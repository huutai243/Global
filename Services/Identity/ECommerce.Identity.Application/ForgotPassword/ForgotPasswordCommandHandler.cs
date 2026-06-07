using ECommerce.Identity.Domain.Models;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Infrastructure.Security.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Identity.Application.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IdentityDbContext dbContext,
    IPasswordResetTokenService passwordResetTokenService,
    IEmailSender emailSender,
    IConfiguration configuration)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.ApplicationUsers
            .FirstOrDefaultAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            return;
        }

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(
                customer => customer.UserId == user.Id,
                cancellationToken);

        if (customer is null)
        {
            return;
        }

        var frontendBaseUrl = configuration["Frontend:BaseUrl"];
        var resetPasswordPath = configuration["Frontend:ResetPasswordPath"];
        var emailSubject = configuration["PasswordReset:EmailSubject"];
        var tokenExpirationMinutes = configuration.GetValue<int>("PasswordReset:TokenExpirationMinutes");

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new InvalidOperationException("Missing configuration: Frontend:BaseUrl.");
        }

        if (string.IsNullOrWhiteSpace(resetPasswordPath))
        {
            throw new InvalidOperationException("Missing configuration: Frontend:ResetPasswordPath.");
        }

        if (string.IsNullOrWhiteSpace(emailSubject))
        {
            throw new InvalidOperationException("Missing configuration: PasswordReset:EmailSubject.");
        }

        if (tokenExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("Invalid configuration: PasswordReset:TokenExpirationMinutes.");
        }

        var existingTokens = await dbContext.PasswordResetTokens
            .Where(token =>
                token.CustomerId == customer.Id &&
                token.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var existingToken in existingTokens)
        {
            existingToken.UsedAt = DateTime.UtcNow;
        }

        var rawToken = passwordResetTokenService.GenerateToken();
        var tokenHash = passwordResetTokenService.HashToken(rawToken);

        var passwordResetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpirationMinutes)
        };

        dbContext.PasswordResetTokens.Add(passwordResetToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var resetUrl =
            $"{frontendBaseUrl.TrimEnd('/')}/{resetPasswordPath.TrimStart('/')}" +
            $"?email={Uri.EscapeDataString(user.Email)}" +
            $"&token={Uri.EscapeDataString(rawToken)}";

        var emailBody = $"""
            <p>Hello {user.FullName},</p>
            <p>You requested to reset your password.</p>
            <p>
                <a href="{resetUrl}">Click here to reset your password</a>
            </p>
            <p>This link will expire in {tokenExpirationMinutes} minutes.</p>
            <p>If you did not request this, please ignore this email.</p>
            """;

        await emailSender.SendAsync(
            user.Email,
            emailSubject,
            emailBody,
            cancellationToken);
    }
}
