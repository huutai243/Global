namespace ECommerce.Domain.Core.Cart.Responses;

public sealed record CartResponse(
    Guid CartId,
    Guid CustomerId,
    decimal TotalAmount,
    IReadOnlyCollection<CartItemResponse> Items);