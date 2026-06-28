namespace ECommerce.Payment.Worker.Options;

public sealed class PayOrderKafkaConsumerOptions
{
    public const string SectionName = "PayOrderKafkaConsumer";

    public string TopicName { get; init; } =
        "ordering.outbox.ECommerce.Shared.Contracts.Payment.PayOrderCommand";

    public string GroupId { get; init; } = "payment.pay-order";
}