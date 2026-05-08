namespace ECommerce.Ordering.Core.Models;

public class Cart
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public List<CartItem> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
