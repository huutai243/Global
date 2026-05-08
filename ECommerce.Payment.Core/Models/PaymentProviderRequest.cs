namespace ECommerce.Payment.Core.Models;

public sealed record PaymentProviderRequest(Guid OrderId, decimal Amount, string Currency, string PaymentMethod);
