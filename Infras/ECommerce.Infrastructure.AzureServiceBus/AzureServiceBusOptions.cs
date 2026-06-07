namespace ECommerce.Infrastructure.AzureServiceBus;

public sealed class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string TopicName { get; init; } = string.Empty;
}