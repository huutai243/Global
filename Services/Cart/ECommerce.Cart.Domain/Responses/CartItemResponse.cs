namespace ECommerce.Cart.Domain.Responses;

public sealed record CartItemResponse(
    Guid CartItemId,
    Guid ProductId,
    string ProductName,
    string? ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);