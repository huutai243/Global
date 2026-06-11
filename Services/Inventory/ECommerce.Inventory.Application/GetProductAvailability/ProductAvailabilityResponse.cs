namespace ECommerce.Inventory.Application.GetProductAvailability;

public sealed record ProductAvailabilityResponse(
    Guid ProductId,
    int AvailableQuantity,
    int ReservedQuantity,
    string StockStatus);