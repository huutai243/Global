namespace ECommerce.Payment.Domain.Models;

// TODO LEDGER:
// These statuses describe payment processing state only.
// They do not prove balanced accounting entries, settlement, refunds, chargebacks, or internal balance movement.
public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2
}
