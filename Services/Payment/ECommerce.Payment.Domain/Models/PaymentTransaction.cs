namespace ECommerce.Payment.Domain.Models;

public sealed class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "VND";

    public PaymentTransactionStatus Status { get; set; }

    public string Provider { get; set; } = "Stripe";

    public string? ProviderTransactionId { get; set; }

    public string? PaymentUrl { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string? FailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}