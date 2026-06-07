using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Cart.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Cart.Application.ClearCart;

public sealed class ClearCartCommandHandler(
    CartDbContext dbContext,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentCustomerId();

        var cart = await dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken);

        if (cart is null)
        {
            return;
        }

        cart.Items.Clear();
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
