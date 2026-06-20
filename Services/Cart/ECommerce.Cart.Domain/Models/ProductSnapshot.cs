namespace ECommerce.Cart.Domain.Models;

public sealed record ProductSnapshot(
    Guid ProductId,
    string ProductName,
    string? ProductImageUrl,
    decimal UnitPrice,
    bool IsActive);