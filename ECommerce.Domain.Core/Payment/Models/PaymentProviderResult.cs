namespace ECommerce.Domain.Core.Payment.Models;

public sealed record PaymentProviderResult(bool IsSuccess, string? ProviderTransactionId, string? FailureReason);
