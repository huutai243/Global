namespace ECommerce.Cart.Domain.Responses;

public sealed record CartResponse(
    Guid CartId,
    Guid CustomerId,
    decimal TotalAmount,
    IReadOnlyCollection<CartItemResponse> Items);