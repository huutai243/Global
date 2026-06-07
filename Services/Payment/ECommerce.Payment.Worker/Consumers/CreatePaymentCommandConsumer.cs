using System.Text.Json;
using ECommerce.Shared.Contracts;
using ECommerce.Payment.Infrastructure.Persistence;
using ECommerce.Payment.Domain.Models;
using ECommerce.Shared.Inbox;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Worker.Consumers;

public sealed class CreatePaymentCommandConsumer(PaymentDbContext dbContext, ILogger<CreatePaymentCommandConsumer> logger)
{
    public async Task HandleAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (await HasProcessedAsync(nameof(CreatePaymentCommand), command.OrderId, cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var payment = await dbContext.Payments
            .FirstOrDefaultAsync(item => item.OrderId == command.OrderId && item.CustomerId == command.CustomerId, cancellationToken);

        if (payment is null)
        {
            payment = new ECommerce.Payment.Domain.Models.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = command.OrderId,
                CustomerId = command.CustomerId,
                Amount = command.Amount,
                Provider = "FakePaymentProvider",
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Payments.Add(payment);
        }

        AddOutboxMessage(new PaymentCreatedEvent(payment.Id, payment.OrderId, payment.CustomerId, payment.Amount, DateTime.UtcNow));
        MarkProcessed(nameof(CreatePaymentCommand), command.OrderId);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Created pending payment {PaymentId} for order {OrderId}", payment.Id, command.OrderId);
    }

    private async Task<bool> HasProcessedAsync(string messageType, Guid aggregateId, CancellationToken cancellationToken)
    {
        var key = BuildKey(messageType, aggregateId);
        return await dbContext.InboxMessages.AnyAsync(
            record => record.MessageId == key && record.ConsumerName == nameof(CreatePaymentCommandConsumer),
            cancellationToken);
    }

    private void MarkProcessed(string messageType, Guid aggregateId)
    {
        dbContext.InboxMessages.Add(new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = BuildKey(messageType, aggregateId),
            CorrelationId = aggregateId.ToString("N"),
            CausationId = aggregateId.ToString("N"),
            MessageType = messageType,
            ConsumerName = nameof(CreatePaymentCommandConsumer),
            Payload = string.Empty,
            Status = InboxMessageStatus.Processed,
            ReceivedAtUtc = DateTime.UtcNow,
            ProcessedAtUtc = DateTime.UtcNow
        });
    }

    private void AddOutboxMessage(object message)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = Guid.NewGuid().ToString("N"),
            CausationId = Guid.NewGuid().ToString("N"),
            MessageType = message.GetType().FullName ?? message.GetType().Name,
            SourceService = "Payment",
            Destination = "Ordering",
            Payload = JsonSerializer.Serialize(message),
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static string BuildKey(string messageType, Guid aggregateId) => $"Payment:{messageType}:{aggregateId:N}";
}
