using ECommerce.Shared.Contracts;

namespace ECommerce.Notification.Worker.Consumers;

public sealed class PaymentSucceededEventConsumer(ILogger<PaymentSucceededEventConsumer> logger)
{
    public Task HandleAsync(PaymentSucceededEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notification queued for successful payment {PaymentId} on order {OrderId}",
            message.PaymentId,
            message.OrderId);

        return Task.CompletedTask;
    }
}
