namespace ECommerce.Ordering.Infrastructure.Clients.Options;

public sealed class CartClientOptions
{
    public const string SectionName = "CartClient";

    public string BaseAddress { get; init; } = string.Empty;
    public string CheckoutSnapshotPath { get; init; } = string.Empty;
}