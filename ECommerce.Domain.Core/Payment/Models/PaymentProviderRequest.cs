namespace ECommerce.Domain.Core.Payment.Models;

public sealed record PaymentProviderRequest(Guid OrderId, decimal Amount, string Currency, string PaymentMethod);
