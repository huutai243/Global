using ECommerce.Shared.Contracts;

namespace ECommerce.Notification.Worker.Consumers;

public sealed class PaymentFailedEventConsumer(ILogger<PaymentFailedEventConsumer> logger)
{
    public Task HandleAsync(PaymentFailedEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notification queued for failed payment {PaymentId} on order {OrderId}: {Reason}",
            message.PaymentId,
            message.OrderId,
            message.Reason);

        return Task.CompletedTask;
    }
}
