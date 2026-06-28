namespace ECommerce.Payment.Application.PayOrder;

public sealed record InitiatePaymentResponse(Guid PaymentId, Guid OrderId, Guid CustomerId, decimal Amount, string Status);
