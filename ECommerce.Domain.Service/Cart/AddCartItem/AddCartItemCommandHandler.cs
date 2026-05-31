using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Cart.Models;
using ECommerce.Domain.Core.Cart.Responses;
using ECommerce.Domain.Core.Catalog.Models;
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
        var customerId = GetCurrentCustomerId();

        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        var product = await dbContext.Products
            .FirstOrDefaultAsync(product => product.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        if (product.Status != ProductStatus.Active)
        {
            throw new BusinessRuleException("Cannot add inactive product to cart.");
        }

        var inventory = await dbContext.InventoryItems
            .FirstOrDefaultAsync(inventoryItem => inventoryItem.ProductId == product.Id, cancellationToken)
            ?? throw new BusinessRuleException("Product inventory is missing.");

        if (inventory.AvailableQuantity < request.Quantity)
        {
            throw new BusinessRuleException("Insufficient stock.");
        }

        var cart = await dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken);

        if (cart is null)
        {
            cart = new ECommerce.Domain.Core.Cart.Models.Cart
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Carts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(cartItem => cartItem.ProductId == product.Id);

        if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                ProductImageUrlSnapshot = product.ImageUrl,
                UnitPriceSnapshot = product.Price,
                Quantity = request.Quantity
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