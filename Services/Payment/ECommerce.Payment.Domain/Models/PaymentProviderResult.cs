namespace ECommerce.Payment.Domain.Models;

public sealed record PaymentProviderResult(bool IsSuccess, string? ProviderTransactionId, string? FailureReason);
