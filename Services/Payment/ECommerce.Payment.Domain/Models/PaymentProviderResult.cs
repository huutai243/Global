namespace ECommerce.Payment.Domain.Models;

public sealed record PaymentProviderResult(
    string ProviderTransactionId,
    string? PaymentUrl);