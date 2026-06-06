using System.Text.Json;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Catalog.Domain.Models;
using ECommerce.Ordering.Domain.Models;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Events;
using ECommerce.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Ordering.Application.CheckoutCart;

public sealed class CheckoutCartCommandHandler(
    ECommerceDbContext dbContext,
    ICurrentUserContext currentUserContext,
    ILogger<CheckoutCartCommandHandler> logger)
    : IRequestHandler<CheckoutCartCommand, CheckoutResponse>
{
    public async Task<CheckoutResponse> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentCustomerId();

        logger.LogInformation("Checkout started for customer {CustomerId}", customerId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var cart = await dbContext.Carts
                .Include(cart => cart.Items)
                .FirstOrDefaultAsync(cart => cart.CustomerId == customerId, cancellationToken)
                ?? throw new BusinessRuleException("Cannot checkout missing cart.");

            if (cart.Items.Count == 0)
            {
                throw new BusinessRuleException("Cannot checkout empty cart.");
            }

            var productIds = cart.Items
                .Select(cartItem => cartItem.ProductId)
                .ToArray();

            var products = await dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, cancellationToken);

            foreach (var cartItem in cart.Items)
            {
                if (!products.TryGetValue(cartItem.ProductId, out var product) ||
                    product.Status != ProductStatus.Active)
                {
                    throw new BusinessRuleException("Cannot checkout inactive product.");
                }
            }

            var orderId = Guid.NewGuid();
            var order = new Order
            {
                Id = orderId,
                CustomerId = customerId,
                Status = OrderStatus.PendingInventoryReservation,
                CreatedAt = DateTime.UtcNow,
                Items = cart.Items
                    .Select(cartItem => new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        ProductId = cartItem.ProductId,
                        ProductNameSnapshot = cartItem.ProductNameSnapshot,
                        UnitPriceSnapshot = cartItem.UnitPriceSnapshot,
                        Quantity = cartItem.Quantity,
                        LineTotal = cartItem.LineTotal
                    })
                    .ToList()
            };

            order.TotalAmount = order.Items.Sum(orderItem => orderItem.LineTotal);

            dbContext.Orders.Add(order);

            dbContext.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;

            var orderCreatedEvent = new OrderCreatedEvent(
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                DateTime.UtcNow);

            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = nameof(OrderCreatedEvent),
                Payload = JsonSerializer.Serialize(orderCreatedEvent),
                Status = OutboxStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            var reserveInventoryCommand = new ReserveInventoryCommand(
                order.Id,
                order.CustomerId,
                order.Items
                    .Select(orderItem => new InventoryReservationItem(
                        orderItem.ProductId,
                        orderItem.ProductNameSnapshot,
                        orderItem.Quantity))
                    .ToArray(),
                DateTime.UtcNow);

            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = nameof(ReserveInventoryCommand),
                Payload = JsonSerializer.Serialize(reserveInventoryCommand),
                Status = OutboxStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Checkout succeeded for customer {CustomerId} and order {OrderId}",
                customerId,
                order.Id);

            return new CheckoutResponse(
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                order.Status.ToString());
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogWarning(
                exception,
                "Checkout concurrency conflict for customer {CustomerId}",
                customerId);

            throw new BusinessRuleException("Product stock changed. Please try checkout again.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
