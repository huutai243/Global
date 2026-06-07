namespace ECommerce.Shared.Messaging;

public sealed record MessageMetadata(string MessageId, string CorrelationId, string CausationId, DateTime OccurredAtUtc);