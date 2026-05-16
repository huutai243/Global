namespace ECommerce.Domain.Service.Ordering.CheckoutCart;

public sealed record CheckoutResponse(Guid OrderId, Guid CustomerId, decimal TotalAmount, string Status);
