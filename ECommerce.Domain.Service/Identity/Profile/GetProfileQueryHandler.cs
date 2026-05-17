using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Service.Identity.Shared;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Domain.Service.Identity.Profile;

public sealed class GetProfileQueryHandler(
    ECommerceDbContext dbContext,
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