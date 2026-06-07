using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Identity.Application.Shared;
using ECommerce.Identity.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Identity.Application.Profile;

public sealed class GetProfileQueryHandler(
    IdentityDbContext dbContext,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await dbContext.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                applicationUser => applicationUser.Id == currentUserContext.UserId.Value,
                cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleException("User does not exist.");
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                customer => customer.UserId == user.Id,
                cancellationToken);

        return new ProfileResponse(
            user.Id,
            customer?.Id,
            user.Email,
            user.FullName,
            user.Role);
    }
}
