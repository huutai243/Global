namespace ECommerce.Ordering.Domain.Models;

public enum OrderStatus
{
    PendingPayment = 0,
    Paid = 1,
    PaymentFailed = 2,
    Cancelled = 3,
    PendingInventoryReservation = 4,
    InventoryFailed = 5
}
