using ECommerce.Cart.Domain.Contracts.Catalog;
using ECommerce.Cart.Domain.Models;
using ECommerce.Cart.Domain.Responses;
using ECommerce.Cart.Infrastructure.Persistence;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Cart.Application.AddCartItem;

public sealed class AddCartItemCommandHandler(
    CartDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IProductSnapshotClient productSnapshotClient,
    ILogger<AddCartItemCommandHandler> logger)
    : IRequestHandler<AddCartItemCommand, CartResponse>
{
    public async Task<CartResponse> Handle(
        AddCartItemCommand request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var customerId = GetCurrentCustomerId();

        var productSnapshot = await GetRequiredProductSnapshotAsync(
            request.ProductId,
            cancellationToken);

        var cart = await GetOrCreateCartAsync(customerId, cancellationToken);

        AddOrUpdateCartItem(cart, productSnapshot, request.Quantity);

        cart.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Added cart item for customer {CustomerId} and product {ProductId}",
            customerId,
            request.ProductId);

        return CartMapper.MapToResponse(cart);
    }

    private static void ValidateRequest(AddCartItemCommand request)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new BusinessRuleException("Product is required.");
        }

        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }
    }

    private Guid GetCurrentCustomerId()
    {
        if (currentUserContext.CustomerId is null ||
            currentUserContext.CustomerId == Guid.Empty)
        {
            throw new ForbiddenAccessException("Customer context is missing.");
        }

        return currentUserContext.CustomerId.Value;
    }

    private async Task<ProductSnapshot> GetRequiredProductSnapshotAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var productSnapshot = await productSnapshotClient.GetProductSnapshotAsync(
            productId,
            cancellationToken);

        if (productSnapshot is null)
        {
            throw new BusinessRuleException("Product was not found.");
        }

        if (!productSnapshot.IsActive)
        {
            throw new BusinessRuleException("Product is not available.");
        }

        return productSnapshot;
    }

    private async Task<Domain.Models.Cart> GetOrCreateCartAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var cart = await dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(
                cart => cart.CustomerId == customerId,
                cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Domain.Models.Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Carts.Add(cart);

        return cart;
    }

    private static void AddOrUpdateCartItem(
        Domain.Models.Cart cart,
        ProductSnapshot productSnapshot,
        int quantity)
    {
        var existingItem = cart.Items
            .FirstOrDefault(cartItem => cartItem.ProductId == productSnapshot.ProductId);

        if (existingItem is null)
        {
            AddNewCartItem(cart, productSnapshot, quantity);

            return;
        }

        UpdateExistingCartItem(existingItem, productSnapshot, quantity);
    }

    private static void AddNewCartItem(
        Domain.Models.Cart cart,
        ProductSnapshot productSnapshot,
        int quantity)
    {
        cart.Items.Add(new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductId = productSnapshot.ProductId,
            ProductNameSnapshot = productSnapshot.ProductName,
            ProductImageUrlSnapshot = productSnapshot.ProductImageUrl,
            UnitPriceSnapshot = productSnapshot.UnitPrice,
            Quantity = quantity
        });
    }

    private static void UpdateExistingCartItem(
        CartItem existingItem,
        ProductSnapshot productSnapshot,
        int quantity)
    {
        existingItem.ProductNameSnapshot = productSnapshot.ProductName;
        existingItem.ProductImageUrlSnapshot = productSnapshot.ProductImageUrl;
        existingItem.UnitPriceSnapshot = productSnapshot.UnitPrice;
        existingItem.Quantity += quantity;
    }
}