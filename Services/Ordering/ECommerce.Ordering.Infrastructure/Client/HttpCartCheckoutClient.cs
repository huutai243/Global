using ECommerce.Ordering.Domain.Contracts.Cart;
using ECommerce.Ordering.Infrastructure.Clients.Options;
using ECommerce.Shared.Core.Exceptions;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerce.Ordering.Infrastructure.Clients;

public sealed class HttpCartCheckoutClient(
    HttpClient httpClient,
    IOptions<CartClientOptions> options)
    : ICartCheckoutClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly CartClientOptions _options = options.Value;

    public async Task<CheckoutCartSnapshot> GetCheckoutSnapshotAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(_options.CheckoutSnapshotPath, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new BusinessRuleException(
                $"Unable to retrieve checkout cart snapshot. Cart service returned {(int)response.StatusCode}.");
        }

        var cart = await response.Content.ReadFromJsonAsync<CartSnapshotResponse>(
            SerializerOptions,
            cancellationToken);

        if (cart is null)
        {
            throw new BusinessRuleException("Cart service returned an empty checkout snapshot.");
        }

        return MapToSnapshot(cart);
    }

    private static CheckoutCartSnapshot MapToSnapshot(CartSnapshotResponse cart)
    {
        return new CheckoutCartSnapshot(
            cart.CustomerId,
            cart.Items.Select(MapToSnapshotItem).ToArray());
    }

    private static CheckoutCartItemSnapshot MapToSnapshotItem(CartSnapshotItemResponse item)
    {
        return new CheckoutCartItemSnapshot(
            item.ProductId,
            item.ProductName,
            item.UnitPrice,
            item.Quantity,
            item.LineTotal);
    }

    private sealed record CartSnapshotResponse(
        Guid CartId,
        Guid CustomerId,
        decimal TotalAmount,
        IReadOnlyCollection<CartSnapshotItemResponse> Items);

    private sealed record CartSnapshotItemResponse(
        Guid CartItemId,
        Guid ProductId,
        string ProductName,
        string? ProductImageUrl,
        decimal UnitPrice,
        int Quantity,
        decimal LineTotal);
}