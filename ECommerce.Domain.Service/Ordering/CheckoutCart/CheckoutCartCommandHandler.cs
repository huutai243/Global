using System.Text.Json;
using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Ordering.Models;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Events;
using ECommerce.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Ordering.CheckoutCart;

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
