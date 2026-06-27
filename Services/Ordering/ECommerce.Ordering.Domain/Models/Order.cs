namespace ECommerce.Ordering.Domain.Models;

public class Order
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    // AUDIT NOTE:
    // Order status is a critical business state, but this entity stores only the current value.
    // Banking-grade systems should persist a status-history/audit table with actor, old value, new value, correlation id, and timestamp.
    public OrderStatus Status { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
