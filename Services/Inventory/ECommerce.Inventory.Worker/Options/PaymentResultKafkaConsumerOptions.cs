namespace ECommerce.Inventory.Worker.Options;

public sealed class PaymentResultKafkaConsumerOptions
{
    public const string SectionName = "PaymentResultKafkaConsumer";

    public string SucceededTopicName { get; init; } =
        "payment.outbox.ECommerce.Shared.Contracts.Payment.PaymentSucceededEvent";

    public string FailedTopicName { get; init; } =
        "payment.outbox.ECommerce.Shared.Contracts.Payment.PaymentFailedEvent";

    public string GroupId { get; init; } = "inventory.payment-result";
}