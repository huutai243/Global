namespace ECommerce.Payment.Core.Models;

public sealed record PaymentProviderResult(bool IsSuccess, string? ProviderTransactionId, string? FailureReason);
