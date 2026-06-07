using System.Text.Json;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Core.Exceptions;
using ECommerce.Shared.Core.Interfaces;
using ECommerce.Payment.Domain.Interfaces;
using ECommerce.Payment.Domain.Models;
using ECommerce.Payment.Infrastructure.Persistence;
using ECommerce.Shared.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Payment.Application.PayOrder;

public sealed class PayOrderCommandHandler(
    PaymentDbContext dbContext,
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

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(
                item => item.OrderId == request.OrderId &&
                        item.CustomerId == request.CustomerId &&
                        item.Status == PaymentStatus.Pending,
                cancellationToken)
            ?? throw new NotFoundException("Pending payment was not found.");

        if (payment.Amount != request.Amount)
        {
            throw new BusinessRuleException("Payment amount must match pending payment total.");
        }

        var providerResult = await paymentProvider.PayAsync(
            new PaymentProviderRequest(payment.OrderId, request.Amount, "USD", request.PaymentMethod),
            cancellationToken);

        payment.ProviderTransactionId = providerResult.ProviderTransactionId;
        payment.Status = providerResult.IsSuccess ? PaymentStatus.Succeeded : PaymentStatus.Failed;

        object eventPayload = providerResult.IsSuccess
            ? new PaymentSucceededEvent(payment.Id, payment.OrderId, payment.CustomerId, payment.Amount, DateTime.UtcNow)
            : new PaymentFailedEvent(
                payment.Id,
                payment.OrderId,
                payment.CustomerId,
                payment.Amount,
                providerResult.FailureReason ?? "Payment failed.",
                DateTime.UtcNow);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = request.OrderId.ToString("N"),
            CausationId = request.OrderId.ToString("N"),
            MessageType = eventPayload.GetType().FullName ?? eventPayload.GetType().Name,
            SourceService = "Payment",
            Destination = "Ordering",
            Payload = JsonSerializer.Serialize(eventPayload),
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Payment {PaymentStatus} for order {OrderId}", payment.Status, payment.OrderId);
        return new PayOrderResponse(payment.Id, payment.OrderId, payment.CustomerId, payment.Amount, payment.Status.ToString());
    }
}
