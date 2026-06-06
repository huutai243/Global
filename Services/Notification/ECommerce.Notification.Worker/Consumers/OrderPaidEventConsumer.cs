using ECommerce.Shared.Contracts;

namespace ECommerce.Notification.Worker.Consumers;

public sealed class OrderPaidEventConsumer(ILogger<OrderPaidEventConsumer> logger)
{
    public Task HandleAsync(OrderPaidEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notification queued for paid order {OrderId} and customer {CustomerId}",
            message.OrderId,
            message.CustomerId);

        return Task.CompletedTask;
    }
}
