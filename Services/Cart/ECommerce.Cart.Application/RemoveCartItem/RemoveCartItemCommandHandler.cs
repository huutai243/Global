using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Cart.Application.RemoveCartItem;

public sealed class RemoveCartItemCommandHandler(
    CartDbContext dbContext,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<RemoveCartItemCommand>
{
    public async Task Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentCustomerId();

        var cart = await dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException("Cart was not found.");

        var cartItem = cart.Items.FirstOrDefault(item => item.Id == request.CartItemId)
            ?? throw new NotFoundException("Cart item was not found.");

        cart.Items.Remove(cartItem);
        cart.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
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
