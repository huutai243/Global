using ECommerce.Ordering.Domain.Contracts.Cart;
using ECommerce.Shared.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerce.Ordering.Infrastructure.Clients;

public sealed class HttpCartCheckoutClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor)
    : ICartCheckoutClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<CheckoutCartSnapshot> GetCheckoutSnapshotAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/cart/checkout-snapshot");

        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authorization)
            && AuthenticationHeaderValue.TryParse(authorization, out var authorizationHeader))
        {
            request.Headers.Authorization = authorizationHeader;
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);

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

        return new CheckoutCartSnapshot(
            cart.CustomerId,
            cart.Items
                .Select(item => new CheckoutCartItemSnapshot(
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal))
                .ToArray());
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