namespace ECommerce.Shared.Contracts;

public sealed record ProductCreatedEvent(
    Guid ProductId,
    string ProductName,
    int InitialStock,
    DateTime OccurredAtUtc);