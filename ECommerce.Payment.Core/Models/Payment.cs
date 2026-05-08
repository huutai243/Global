namespace ECommerce.Payment.Core.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? ProviderTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
