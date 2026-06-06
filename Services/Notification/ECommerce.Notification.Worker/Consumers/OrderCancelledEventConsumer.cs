using ECommerce.Shared.Contracts;

namespace ECommerce.Notification.Worker.Consumers;

public sealed class OrderCancelledEventConsumer(ILogger<OrderCancelledEventConsumer> logger)
{
    public Task HandleAsync(OrderCancelledEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notification queued for cancelled order {OrderId} and customer {CustomerId}: {Reason}",
            message.OrderId,
            message.CustomerId,
            message.Reason);

        return Task.CompletedTask;
    }
}
