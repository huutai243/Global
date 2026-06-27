namespace ECommerce.Payment.Domain.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid CustomerId { get; set; }

    public decimal Amount { get; set; }

    // AUDIT NOTE:
    // Payment status changes are business-critical and should be auditable.
    // A real audit trail should record actor/system, old status, new status, provider reference, correlation id, and timestamp.
    public PaymentStatus Status { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? ProviderTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // TODO LEDGER:
    // Payment status alone is not a double-entry ledger.
    // If this service manages money/balances, add LedgerAccount and LedgerEntry with debit/credit entries balanced per transaction.
}
