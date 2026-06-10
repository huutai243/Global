namespace ECommerce.Inventory.Domain.Models;

public sealed class StockReservationItem
{
    public Guid Id { get; set; }

    public Guid StockReservationId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductNameSnapshot { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public StockReservation StockReservation { get; set; } = default!;
}