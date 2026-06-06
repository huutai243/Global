namespace ECommerce.Ordering.Application.CheckoutCart;

public sealed record CheckoutResponse(Guid OrderId, Guid CustomerId, decimal TotalAmount, string Status);
