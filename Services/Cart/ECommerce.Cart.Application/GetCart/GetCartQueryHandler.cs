using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Cart.Domain.Responses;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Cart.Application.GetCart;

public sealed class GetCartQueryHandler(
    ECommerceDbContext dbContext,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetCartQuery, CartResponse>
{
    public async Task<CartResponse> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentCustomerId();

        var cart = await dbContext.Carts
            .AsNoTracking()
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken);

        if (cart is null)
        {
            return new CartResponse(
                Guid.Empty,
                customerId,
                0,
                []);
        }

        return CartMapper.MapToResponse(cart);
    }

    private Guid GetCurrentCustomerId()
    {
        if (currentUserContext.CustomerId is null || currentUserContext.CustomerId == Guid.Empty)
        {
            throw new ForbiddenAccessException("Customer context is missing.");
        }

        return currentUserContext.CustomerId.Value;
    }
}