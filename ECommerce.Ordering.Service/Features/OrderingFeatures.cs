using System.Text.Json;
using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Cart.Models;
using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Identity.Models;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Events;
using ECommerce.Infrastructure.Persistence.Models;
using ECommerce.Domain.Core.Inventory.Models;
using ECommerce.Domain.Core.Ordering.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Ordering.Service.Features;

public sealed record AddCartItemCommand(Guid CustomerId, Guid ProductId, int Quantity) : IRequest<CartResponse>;

public sealed record CheckoutCartCommand(Guid CustomerId, string IdempotencyKey) : IRequest<CheckoutResponse>, IIdempotentCommand;

public sealed record CartResponse(Guid CartId, Guid CustomerId, decimal TotalAmount, IReadOnlyCollection<CartItemResponse> Items);

public sealed record CartItemResponse(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record CheckoutResponse(Guid OrderId, Guid CustomerId, decimal TotalAmount, string Status);

public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThan(0);
    }
}

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
    }
}

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
            cart = new Cart
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

    private static CartResponse MapCart(Cart cart)
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

public sealed class CheckoutCartCommandHandler(
    ECommerceDbContext dbContext,
    ICurrentUserContext currentUserContext,
    ILogger<CheckoutCartCommandHandler> logger)
    : IRequestHandler<CheckoutCartCommand, CheckoutResponse>
{
    public async Task<CheckoutResponse> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserContext.IsAdmin && currentUserContext.CustomerId != request.CustomerId)
        {
            throw new ForbiddenAccessException("Customer can only checkout own cart.");
        }

        logger.LogInformation("Checkout started for customer {CustomerId}", request.CustomerId);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var cart = await dbContext.Carts
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.CustomerId == request.CustomerId, cancellationToken)
            ?? throw new BusinessRuleException("Cannot checkout missing cart.");

        if (cart.Items.Count == 0)
        {
            throw new BusinessRuleException("Cannot checkout empty cart.");
        }

        var productIds = cart.Items.Select(item => item.ProductId).ToArray();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        var inventoryItems = await dbContext.InventoryItems
            .Where(inventoryItem => productIds.Contains(inventoryItem.ProductId))
            .ToDictionaryAsync(inventoryItem => inventoryItem.ProductId, cancellationToken);

        foreach (var cartItem in cart.Items)
        {
            if (!products.TryGetValue(cartItem.ProductId, out var product) || product.Status != ProductStatus.Active)
            {
                throw new BusinessRuleException("Cannot checkout inactive product.");
            }

            if (!inventoryItems.TryGetValue(cartItem.ProductId, out var inventoryItem) ||
                inventoryItem.AvailableQuantity < cartItem.Quantity)
            {
                throw new BusinessRuleException("Insufficient stock.");
            }

            inventoryItem.AvailableQuantity -= cartItem.Quantity;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.PendingPayment,
            CreatedAt = DateTime.UtcNow,
            Items = cart.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ProductNameSnapshot = item.ProductNameSnapshot,
                UnitPriceSnapshot = item.UnitPriceSnapshot,
                Quantity = item.Quantity,
                LineTotal = item.LineTotal
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(item => item.LineTotal);

        dbContext.Orders.Add(order);
        dbContext.CartItems.RemoveRange(cart.Items);
        cart.UpdatedAt = DateTime.UtcNow;

        var @event = new OrderCreatedEvent(order.Id, order.CustomerId, order.TotalAmount, DateTime.UtcNow);
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(OrderCreatedEvent),
            Payload = JsonSerializer.Serialize(@event),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Checkout succeeded for customer {CustomerId} and order {OrderId}", request.CustomerId, order.Id);
        return new CheckoutResponse(order.Id, order.CustomerId, order.TotalAmount, order.Status.ToString());
    }
}
