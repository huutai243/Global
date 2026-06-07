using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Cart.Infrastructure.Persistence;
using ECommerce.Cart.Domain.Models;
using ECommerce.Cart.Domain.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Cart.Application.AddCartItem;

public sealed class AddCartItemCommandHandler(
    CartDbContext dbContext,
    ICurrentUserContext currentUserContext,
    ILogger<AddCartItemCommandHandler> logger)
    : IRequestHandler<AddCartItemCommand, CartResponse>
{
    public async Task<CartResponse> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentCustomerId();

        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        // TODO: Boundary violation removed. Replace with Catalog product snapshot and Inventory availability contracts.
        if (string.IsNullOrWhiteSpace(request.ProductNameSnapshot) || request.UnitPriceSnapshot is null)
        {
            throw new BusinessRuleException("Product snapshot is required before adding an item to the cart.");
        }

        var cart = await dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken);

        if (cart is null)
        {
            cart = new ECommerce.Cart.Domain.Models.Cart
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Carts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(cartItem => cartItem.ProductId == request.ProductId);

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = request.ProductId,
                ProductNameSnapshot = request.ProductNameSnapshot,
                ProductImageUrlSnapshot = request.ProductImageUrlSnapshot,
                UnitPriceSnapshot = request.UnitPriceSnapshot.Value,
                Quantity = request.Quantity
            });
        }
        else
        {
            var newQuantity = existingItem.Quantity + request.Quantity;

            existingItem.Quantity = newQuantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Added cart item for customer {CustomerId} and product {ProductId}",
            customerId,
            request.ProductId);

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
