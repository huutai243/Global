using ECommerce.Ordering.Domain.Contracts.Cart;
using ECommerce.Ordering.Domain.Models;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Shared.Outbox;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Ordering.Application.CheckoutCart;

public sealed class CheckoutCartCommandHandler(
    OrderingDbContext dbContext,
    ICartCheckoutClient cartCheckoutClient,
    ICurrentUserContext currentUserContext,
    OutboxMessageFactory outboxMessageFactory,
    ILogger<CheckoutCartCommandHandler> logger)
    : IRequestHandler<CheckoutCartCommand, CheckoutResponse>
{
    private const string SourceService = "Ordering";
    private const string DestinationService = "Inventory";

    public async Task<CheckoutResponse> Handle(
        CheckoutCartCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUserContext.CustomerId is null || currentUserContext.CustomerId == Guid.Empty)
        {
            throw new ForbiddenAccessException("Customer context is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new BusinessRuleException("Checkout idempotency key is required.");
        }

        var customerId = currentUserContext.CustomerId.Value;
        var idempotencyKey = request.IdempotencyKey;

        // Idempotency prevents duplicated orders when the same checkout request is retried.
        var existingOrder = await FindExistingOrderAsync(
            customerId,
            idempotencyKey,
            cancellationToken);

        if (existingOrder is not null)
        {
            logger.LogInformation(
                "Checkout idempotency hit. CustomerId: {CustomerId}, OrderId: {OrderId}, IdempotencyKey: {IdempotencyKey}",
                customerId,
                existingOrder.Id,
                idempotencyKey);

            return MapToResponse(existingOrder);
        }

        // The cart snapshot freezes checkout data such as product name, unit price, and quantity.
        var cart = await cartCheckoutClient.GetCheckoutSnapshotAsync(cancellationToken);

        if (cart.CustomerId != customerId)
        {
            throw new ForbiddenAccessException("Cart snapshot does not belong to the current customer.");
        }

        if (cart.Items.Count == 0)
        {
            throw new BusinessRuleException("Cannot checkout an empty cart.");
        }

        var utcNow = DateTime.UtcNow;

        var order = CreateOrder(
            customerId,
            idempotencyKey,
            cart,
            utcNow);

        var reserveInventoryCommand = CreateReserveInventoryCommand(order, utcNow);

        // Outbox keeps order creation and event publishing reliable across service failures.
        var outboxMessage = outboxMessageFactory.Create(
            reserveInventoryCommand,
            SourceService,
            DestinationService,
            Guid.NewGuid().ToString("N"),
            idempotencyKey,
            utcNow);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            dbContext.Orders.Add(order);
            dbContext.OutboxMessages.Add(outboxMessage);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Checkout committed. OrderId: {OrderId}, OutboxMessageId: {MessageId}, CorrelationId: {CorrelationId}",
                order.Id,
                outboxMessage.MessageId,
                outboxMessage.CorrelationId);

            return MapToResponse(order);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            // Another request may have inserted the same idempotent order concurrently.
            var idempotentOrder = await FindExistingOrderAsync(
                customerId,
                idempotencyKey,
                cancellationToken);

            if (idempotentOrder is null)
            {
                throw;
            }

            logger.LogInformation(
                "Checkout idempotency race resolved. CustomerId: {CustomerId}, OrderId: {OrderId}, IdempotencyKey: {IdempotencyKey}",
                customerId,
                idempotentOrder.Id,
                idempotencyKey);

            return MapToResponse(idempotentOrder);
        }
    }

    private async Task<Order?> FindExistingOrderAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(
                order => order.CustomerId == customerId
                    && order.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    private static Order CreateOrder(
        Guid customerId,
        string idempotencyKey,
        CheckoutCartSnapshot cart,
        DateTime utcNow)
    {
        var orderId = Guid.NewGuid();

        var orderItems = cart.Items
            .Select(cartItem => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = cartItem.ProductId,
                ProductNameSnapshot = cartItem.ProductName,
                UnitPriceSnapshot = cartItem.UnitPrice,
                Quantity = cartItem.Quantity,
                LineTotal = cartItem.LineTotal
            })
            .ToList();

        return new Order
        {
            Id = orderId,
            CustomerId = customerId,
            IdempotencyKey = idempotencyKey,
            TotalAmount = orderItems.Sum(orderItem => orderItem.LineTotal),
            Status = OrderStatus.PendingInventoryReservation,
            Items = orderItems,
            CreatedAt = utcNow
        };
    }

    private static ReserveInventoryCommand CreateReserveInventoryCommand(
        Order order,
        DateTime utcNow)
    {
        var items = order.Items
            .Select(orderItem => new InventoryReservationItem(
                orderItem.ProductId,
                orderItem.ProductNameSnapshot,
                orderItem.Quantity))
            .ToArray();

        return new ReserveInventoryCommand(
            order.Id,
            order.CustomerId,
            items,
            utcNow);
    }

    private static CheckoutResponse MapToResponse(Order order)
    {
        return new CheckoutResponse(
            order.Id,
            order.CustomerId,
            order.TotalAmount,
            order.Status.ToString());
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}