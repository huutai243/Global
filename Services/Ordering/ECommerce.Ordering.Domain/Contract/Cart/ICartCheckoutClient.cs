namespace ECommerce.Ordering.Domain.Contracts.Cart;

public interface ICartCheckoutClient
{
    Task<CheckoutCartSnapshot> GetCheckoutSnapshotAsync(CancellationToken cancellationToken);
}

public sealed record CheckoutCartSnapshot(
    Guid CustomerId,
    IReadOnlyCollection<CheckoutCartItemSnapshot> Items);

public sealed record CheckoutCartItemSnapshot(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);