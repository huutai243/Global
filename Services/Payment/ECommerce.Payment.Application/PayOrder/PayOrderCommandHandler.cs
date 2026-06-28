using ECommerce.Payment.Domain.Interfaces;
using ECommerce.Payment.Domain.Models;
using ECommerce.Payment.Infrastructure.Persistence;
using ECommerce.Shared.Contracts.Payment;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Messaging;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Payment.Application.PayOrder;

public sealed class PayOrderCommandHandler(
    PaymentDbContext dbContext,
    IValidator<PayOrderCommand> validator,
    IPaymentGateway paymentGateway,
    IMessageNameResolver messageNameResolver,
    ILogger<PayOrderCommandHandler> logger)
{
    private const string ConsumerName = "Payment.PayOrder";
    private const string DefaultPaymentMethod = "StripeCheckout";

    public async Task HandleAsync(
        PayOrderCommand command,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        await ProcessAsync(command, metadata, payload, cancellationToken);
    }

    private async Task ProcessAsync(
        PayOrderCommand command,
        MessageMetadata metadata,
        string payload,
        CancellationToken cancellationToken)
    {
        if (await IsProcessedAsync(metadata.MessageId, cancellationToken))
        {
            logger.LogInformation(
                "Pay order message already processed. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                command.OrderId);

            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var inboxMessage = CreateInboxMessage(metadata, payload);
            dbContext.InboxMessages.Add(inboxMessage);

            if (await HasPaymentTransactionAsync(command.OrderId, cancellationToken))
            {
                MarkInboxProcessed(inboxMessage);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "Pay order skipped because payment transaction already exists. MessageId: {MessageId}, OrderId: {OrderId}",
                    metadata.MessageId,
                    command.OrderId);

                return;
            }

            var providerResult = await paymentGateway.CreatePaymentSessionAsync(
                CreateProviderRequest(command),
                cancellationToken);

            var utcNow = DateTime.UtcNow;
            var paymentTransaction = CreatePendingTransaction(command, providerResult, utcNow);

            dbContext.PaymentTransactions.Add(paymentTransaction);

            MarkInboxProcessed(inboxMessage);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Payment session created. MessageId: {MessageId}, OrderId: {OrderId}, PaymentTransactionId: {PaymentTransactionId}, ProviderTransactionId: {ProviderTransactionId}",
                metadata.MessageId,
                command.OrderId,
                paymentTransaction.Id,
                paymentTransaction.ProviderTransactionId);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogInformation(
                exception,
                "Pay order skipped by idempotency constraint. MessageId: {MessageId}, OrderId: {OrderId}",
                metadata.MessageId,
                command.OrderId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private InboxMessage CreateInboxMessage(MessageMetadata metadata, string payload)
    {
        var utcNow = DateTime.UtcNow;

        return new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = metadata.MessageId,
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            MessageType = messageNameResolver.ResolveMessageName(typeof(PayOrderCommand)),
            ConsumerName = ConsumerName,
            Payload = payload,
            Status = InboxMessageStatus.Processing,
            ReceivedAtUtc = utcNow,
            ProcessingStartedAtUtc = utcNow
        };
    }

    private async Task<bool> IsProcessedAsync(
        string messageId,
        CancellationToken cancellationToken)
    {
        return await dbContext.InboxMessages.AnyAsync(
            message => message.MessageId == messageId
                && message.ConsumerName == ConsumerName
                && message.Status == InboxMessageStatus.Processed,
            cancellationToken);
    }

    private async Task<bool> HasPaymentTransactionAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PaymentTransactions.AnyAsync(
            payment => payment.OrderId == orderId,
            cancellationToken);
    }

    private static PaymentProviderRequest CreateProviderRequest(PayOrderCommand command)
    {
        return new PaymentProviderRequest(
            command.OrderId,
            command.CustomerId,
            command.Amount,
            command.Currency,
            command.IdempotencyKey,
            DefaultPaymentMethod);
    }

    private PaymentTransaction CreatePendingTransaction(
        PayOrderCommand command,
        PaymentProviderResult providerResult,
        DateTime utcNow)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = command.OrderId,
            CustomerId = command.CustomerId,
            Amount = command.Amount,
            Currency = command.Currency,
            Status = PaymentTransactionStatus.Pending,
            Provider = paymentGateway.ProviderName,
            ProviderTransactionId = providerResult.ProviderTransactionId,
            PaymentUrl = providerResult.PaymentUrl,
            IdempotencyKey = command.IdempotencyKey,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    private static void MarkInboxProcessed(InboxMessage inboxMessage)
    {
        inboxMessage.Status = InboxMessageStatus.Processed;
        inboxMessage.ProcessedAtUtc = DateTime.UtcNow;
        inboxMessage.ErrorMessage = null;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}