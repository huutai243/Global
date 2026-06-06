using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Identity.Application.Shared;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Security.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Identity.Application.Profile;

public sealed class UpdateProfileCommandHandler(
    ECommerceDbContext dbContext,
    ICurrentUserContext currentUserContext,
    ILogger<UpdateProfileCommandHandler> logger)
    : IRequestHandler<UpdateProfileCommand, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await dbContext.ApplicationUsers
            .FirstOrDefaultAsync(
                applicationUser => applicationUser.Id == currentUserContext.UserId.Value,
                cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleException("User does not exist.");
        }

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(
                customer => customer.UserId == user.Id,
                cancellationToken);

        string normalizedFullName = request.FullName.Trim();

        user.FullName = normalizedFullName;

        if (customer is not null)
        {
            customer.FullName = normalizedFullName;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Profile updated for user {UserId}", user.Id);

        return new ProfileResponse(
            user.Id,
            customer?.Id,
            user.Email,
            user.FullName,
            user.Role);
    }
}