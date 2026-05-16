using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Cart.Models;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Cart.AddCartItem;

public sealed class AddCartItemCommandHandler(
    ECommerceDbContext dbContext,
    ICurrentUserContext currentUserContext,
    ILogger<AddCartItemCommandHandler> logger)
    : IRequestHandler<AddCartItemCommand, CartResponse>
{
    public async Task<CartResponse> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        EnsureCustomerAccess(request.CustomerId, currentUserContext);

        var product = await dbContext.Products
            .FirstOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        if (product.Status != ProductStatus.Active)
        {
            throw new BusinessRuleException("Cannot add inactive product to cart.");
        }

        var inventory = await dbContext.InventoryItems.FirstOrDefaultAsync(item => item.ProductId == product.Id, cancellationToken)
            ?? throw new BusinessRuleException("Product inventory is missing.");

        if (inventory.AvailableQuantity < request.Quantity)
        {
            throw new BusinessRuleException("Insufficient stock.");
        }

        var cart = await dbContext.Carts
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.CustomerId == request.CustomerId, cancellationToken);

        if (cart is null)
        {
            cart = new ECommerce.Domain.Core.Cart.Models.Cart
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Carts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == product.Id);
        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UnitPriceSnapshot = product.Price,
                Quantity = request.Quantity,
                LineTotal = product.Price * request.Quantity
            });
        }
        else
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (inventory.AvailableQuantity < newQuantity)
            {
                throw new BusinessRuleException("Insufficient stock.");
            }

            existingItem.Quantity = newQuantity;
            existingItem.LineTotal = existingItem.UnitPriceSnapshot * newQuantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Add cart item for customer {CustomerId} and product {ProductId}", request.CustomerId, request.ProductId);
        return MapCart(cart);
    }

    private static void EnsureCustomerAccess(Guid customerId, ICurrentUserContext currentUserContext)
    {
        if (!currentUserContext.IsAdmin && currentUserContext.CustomerId != customerId)
        {
            throw new ForbiddenAccessException("Customer can only access own cart.");
        }
    }

    private static CartResponse MapCart(ECommerce.Domain.Core.Cart.Models.Cart cart)
    {
        var items = cart.Items.Select(item => new CartItemResponse(
            item.ProductId,
            item.ProductNameSnapshot,
            item.UnitPriceSnapshot,
            item.Quantity,
            item.LineTotal)).ToArray();

        return new CartResponse(cart.Id, cart.CustomerId, items.Sum(item => item.LineTotal), items);
    }
}
