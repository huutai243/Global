using System.Text.Json;
using ECommerce.Shared.Contracts;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Persistence.Models;
using ECommerce.Payment.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Worker.Consumers;

public sealed class CreatePaymentCommandConsumer(ECommerceDbContext dbContext, ILogger<CreatePaymentCommandConsumer> logger)
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
        return await dbContext.IdempotencyRecords.AnyAsync(record => record.Key == key, cancellationToken);
    }

    private void MarkProcessed(string messageType, Guid aggregateId)
    {
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = BuildKey(messageType, aggregateId),
            RequestHash = aggregateId.ToString("N"),
            Status = "Completed",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
    }

    private void AddOutboxMessage(object message)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = message.GetType().Name,
            Payload = JsonSerializer.Serialize(message),
            Status = OutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string BuildKey(string messageType, Guid aggregateId) => $"Payment:{messageType}:{aggregateId:N}";
}
