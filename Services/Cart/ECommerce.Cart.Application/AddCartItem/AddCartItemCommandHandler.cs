using ECommerce.Cart.Domain.Contracts.Catalog;
using ECommerce.Cart.Domain.Models;
using ECommerce.Cart.Domain.Responses;
using ECommerce.Cart.Infrastructure.Persistence;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using MediatR;
using Microsoft.Data.SqlClient;
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
    private const int MaxRetries = 3;
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlUniqueIndexViolation = 2601;

    public async Task<CartResponse> Handle(
        AddCartItemCommand request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var customerId = GetCurrentCustomerId();

        var productSnapshot = await GetRequiredProductSnapshotAsync(
            request.ProductId,
            cancellationToken);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var cart = await GetOrCreateCartAsync(customerId, cancellationToken);

                var itemWasUpdated = await TryIncrementExistingCartItemAsync(
                    cart.Id,
                    productSnapshot,
                    request.Quantity,
                    cancellationToken);

                if (!itemWasUpdated)
                {
                    AddNewCartItem(cart.Id, productSnapshot, request.Quantity);
                }

                await TouchCartAsync(cart.Id, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Added cart item for customer {CustomerId} and product {ProductId}",
                    customerId,
                    request.ProductId);

                return await GetCartResponseAsync(customerId, cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception) && attempt < MaxRetries)
            {
                logger.LogWarning(
                    exception,
                    "Cart item insert conflicted with another request. CustomerId: {CustomerId}, ProductId: {ProductId}, Attempt: {Attempt}",
                    customerId,
                    request.ProductId,
                    attempt);

                dbContext.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException exception) when (attempt < MaxRetries)
            {
                logger.LogWarning(
                    exception,
                    "Cart update conflicted with another request. CustomerId: {CustomerId}, ProductId: {ProductId}, Attempt: {Attempt}",
                    customerId,
                    request.ProductId,
                    attempt);

                dbContext.ChangeTracker.Clear();
            }
        }

        throw new BusinessRuleException("Cart was changed by another request. Please try again.");
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
        var existingCart = await dbContext.Carts
            .FirstOrDefaultAsync(
                cart => cart.CustomerId == customerId,
                cancellationToken);

        if (existingCart is not null)
        {
            return existingCart;
        }

        var cart = new Domain.Models.Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Carts.Add(cart);

        return cart;
    }

    private async Task<bool> TryIncrementExistingCartItemAsync(
        Guid cartId,
        ProductSnapshot productSnapshot,
        int quantity,
        CancellationToken cancellationToken)
    {
        var affectedRows = await dbContext.CartItems
            .Where(cartItem =>
                cartItem.CartId == cartId &&
                cartItem.ProductId == productSnapshot.ProductId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        cartItem => cartItem.ProductNameSnapshot,
                        productSnapshot.ProductName)
                    .SetProperty(
                        cartItem => cartItem.ProductImageUrlSnapshot,
                        productSnapshot.ProductImageUrl)
                    .SetProperty(
                        cartItem => cartItem.UnitPriceSnapshot,
                        productSnapshot.UnitPrice)
                    .SetProperty(
                        cartItem => cartItem.Quantity,
                        cartItem => cartItem.Quantity + quantity),
                cancellationToken);

        return affectedRows > 0;
    }

    private void AddNewCartItem(
        Guid cartId,
        ProductSnapshot productSnapshot,
        int quantity)
    {
        dbContext.CartItems.Add(new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            ProductId = productSnapshot.ProductId,
            ProductNameSnapshot = productSnapshot.ProductName,
            ProductImageUrlSnapshot = productSnapshot.ProductImageUrl,
            UnitPriceSnapshot = productSnapshot.UnitPrice,
            Quantity = quantity
        });
    }

    private async Task TouchCartAsync(
        Guid cartId,
        CancellationToken cancellationToken)
    {
        await dbContext.Carts
            .Where(cart => cart.Id == cartId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    cart => cart.UpdatedAt,
                    DateTime.UtcNow),
                cancellationToken);
    }

    private async Task<CartResponse> GetCartResponseAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var cart = await dbContext.Carts
            .AsNoTracking()
            .Include(cart => cart.Items)
            .FirstAsync(
                cart => cart.CustomerId == customerId,
                cancellationToken);

        return CartMapper.MapToResponse(cart);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
            sqlException.Number is SqlUniqueConstraintViolation or SqlUniqueIndexViolation;
    }
}