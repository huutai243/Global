namespace ECommerce.Payment.Domain.Models;

public sealed record PaymentProviderRequest(Guid OrderId, decimal Amount, string Currency, string PaymentMethod);
