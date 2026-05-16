namespace ECommerce.Domain.Service.Payment.PayOrder;

public sealed record PayOrderResponse(Guid PaymentId, Guid OrderId, Guid CustomerId, decimal Amount, string Status);
