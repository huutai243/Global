using System.Text.Json;
using ECommerce.Core.SharedLibs.Exceptions;
using ECommerce.Core.SharedLibs.Interfaces;
using ECommerce.Domain.Core.Ordering.Models;
using ECommerce.Domain.Core.Payment.Interfaces;
using ECommerce.Domain.Core.Payment.Models;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Events;
using ECommerce.Infrastructure.Persistence.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Domain.Service.Payment.PayOrder;

public sealed class PayOrderCommandHandler(
    ECommerceDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IPaymentProvider paymentProvider,
    ILogger<PayOrderCommandHandler> logger)
    : IRequestHandler<PayOrderCommand, PayOrderResponse>
{
    public async Task<PayOrderResponse> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserContext.IsAdmin && currentUserContext.CustomerId != request.CustomerId)
        {
            throw new ForbiddenAccessException("Customer can only pay own order.");
        }

        logger.LogInformation("Payment started for order {OrderId}", request.OrderId);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = await dbContext.Orders.FirstOrDefaultAsync(item => item.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        if (order.CustomerId != request.CustomerId)
        {
            throw new ForbiddenAccessException("Customer can only pay own order.");
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new BusinessRuleException("Only pending payment orders can be paid.");
        }

        if (order.TotalAmount != request.Amount)
        {
            throw new BusinessRuleException("Payment amount must match order total.");
        }

        var providerResult = await paymentProvider.PayAsync(
            new PaymentProviderRequest(order.Id, request.Amount, "USD", request.PaymentMethod),
            cancellationToken);

        var payment = new ECommerce.Domain.Core.Payment.Models.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Amount = request.Amount,
            Provider = "FakePaymentProvider",
            ProviderTransactionId = providerResult.ProviderTransactionId,
            Status = providerResult.IsSuccess ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            CreatedAt = DateTime.UtcNow
        };

        order.Status = providerResult.IsSuccess ? OrderStatus.Paid : OrderStatus.PaymentFailed;
        order.UpdatedAt = DateTime.UtcNow;

        dbContext.Payments.Add(payment);
        object eventPayload = providerResult.IsSuccess
            ? new PaymentSucceededEvent(payment.Id, order.Id, order.CustomerId, payment.Amount, DateTime.UtcNow)
            : new PaymentFailedEvent(payment.Id, order.Id, order.CustomerId, payment.Amount, DateTime.UtcNow);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventPayload.GetType().Name,
            Payload = JsonSerializer.Serialize(eventPayload),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Payment {PaymentStatus} for order {OrderId}", payment.Status, order.Id);
        return new PayOrderResponse(payment.Id, order.Id, order.CustomerId, payment.Amount, payment.Status.ToString());
    }
}
