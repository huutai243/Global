using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Cart.Domain.Responses;
using ECommerce.Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Cart.Application.UpdateCartItem;

public sealed class UpdateCartItemCommandHandler(
    CartDbContext dbContext,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<UpdateCartItemCommand, CartResponse>
{
    public async Task<CartResponse> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentCustomerId();

        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        var cart = await dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken)
            ?? throw new NotFoundException("Cart was not found.");

        var cartItem = cart.Items.FirstOrDefault(item => item.Id == request.CartItemId)
            ?? throw new NotFoundException("Cart item was not found.");

        // TODO: Boundary violation removed. Replace with an Inventory availability contract before enforcing stock here.

        cartItem.Quantity = request.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

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
