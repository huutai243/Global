using ECommerce.Payment.Domain.Models;
using ECommerce.Payment.Infrastructure.Persistence;
using ECommerce.Shared.Contracts.Payment;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace ECommerce.Payment.Application.StripeWebhook;

public sealed class StripeWebhookHandler(
    PaymentDbContext dbContext,
    OutboxMessageFactory outboxMessageFactory,
    ILogger<StripeWebhookHandler> logger)
{
    private const string SourceService = "Payment";
    private const string DestinationService = "Ordering";

    public async Task HandleAsync(
        Event stripeEvent,
        CancellationToken cancellationToken)
    {
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken);
                return;

            case "checkout.session.expired":
                await HandleCheckoutExpiredAsync(stripeEvent, cancellationToken);
                return;

            default:
                logger.LogInformation(
                    "Stripe webhook ignored. EventType: {EventType}, EventId: {EventId}",
                    stripeEvent.Type,
                    stripeEvent.Id);
                return;
        }
    }

    private async Task HandleCheckoutCompletedAsync(
        Event stripeEvent,
        CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session
            ?? throw new InvalidOperationException("Stripe checkout.session.completed payload is invalid.");

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Stripe checkout session completed but payment is not paid. SessionId: {SessionId}, PaymentStatus: {PaymentStatus}",
                session.Id,
                session.PaymentStatus);

            return;
        }

        await UpdatePaymentAsync(
            session.Id,
            PaymentTransactionStatus.Succeeded,
            null,
            cancellationToken);
    }

    private async Task HandleCheckoutExpiredAsync(
        Event stripeEvent,
        CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session
            ?? throw new InvalidOperationException("Stripe checkout.session.expired payload is invalid.");

        await UpdatePaymentAsync(
            session.Id,
            PaymentTransactionStatus.Failed,
            "Stripe checkout session expired.",
            cancellationToken);
    }

    private async Task UpdatePaymentAsync(
        string stripeSessionId,
        PaymentTransactionStatus targetStatus,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var payment = await dbContext.PaymentTransactions.FirstOrDefaultAsync(
                payment => payment.ProviderTransactionId == stripeSessionId,
                cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(
                    $"Payment transaction was not found for Stripe session '{stripeSessionId}'.");
            }

            if (payment.Status != PaymentTransactionStatus.Pending)
            {
                logger.LogInformation(
                    "Stripe webhook ignored because payment is not pending. PaymentTransactionId: {PaymentTransactionId}, CurrentStatus: {CurrentStatus}",
                    payment.Id,
                    payment.Status);

                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var utcNow = DateTime.UtcNow;

            payment.Status = targetStatus;
            payment.FailureReason = failureReason;
            payment.UpdatedAtUtc = utcNow;

            dbContext.OutboxMessages.Add(
                targetStatus == PaymentTransactionStatus.Succeeded
                    ? CreateSucceededOutboxMessage(payment, utcNow)
                    : CreateFailedOutboxMessage(payment, failureReason ?? "Payment failed.", utcNow));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private OutboxMessage CreateSucceededOutboxMessage(
        PaymentTransaction payment,
        DateTime utcNow)
    {
        var @event = new PaymentSucceededEvent(
            payment.OrderId,
            payment.CustomerId,
            payment.Id,
            payment.Provider,
            payment.ProviderTransactionId!,
            payment.Amount,
            payment.Currency,
            utcNow);

        return CreateOutboxMessage(@event, payment.IdempotencyKey, utcNow);
    }

    private OutboxMessage CreateFailedOutboxMessage(
        PaymentTransaction payment,
        string failureReason,
        DateTime utcNow)
    {
        var @event = new PaymentFailedEvent(
            payment.OrderId,
            payment.CustomerId,
            payment.Id,
            payment.Provider,
            payment.Amount,
            payment.Currency,
            failureReason,
            utcNow);

        return CreateOutboxMessage(@event, payment.IdempotencyKey, utcNow);
    }

    private OutboxMessage CreateOutboxMessage<TEvent>(
        TEvent @event,
        string correlationId,
        DateTime utcNow)
        where TEvent : class
    {
        return outboxMessageFactory.Create(
            @event,
            SourceService,
            DestinationService,
            Guid.NewGuid().ToString("N"),
            correlationId,
            utcNow);
    }
}