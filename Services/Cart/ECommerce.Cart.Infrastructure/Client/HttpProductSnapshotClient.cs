using ECommerce.Cart.Domain.Contracts.Catalog;
using ECommerce.Cart.Domain.Models;
using ECommerce.Cart.Infrastructure.Client;
using ECommerce.Shared.Core.Exceptions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ECommerce.Cart.Infrastructure.Clients;

public sealed class HttpProductSnapshotClient(
    HttpClient httpClient,
    IOptions<CatalogClientOptions> options)
    : IProductSnapshotClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly CatalogClientOptions _options = options.Value;

    public async Task<ProductSnapshot?> GetProductSnapshotAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var path = BuildProductSnapshotPath(productId);

        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new BusinessRuleException(
                $"Unable to retrieve product snapshot. Catalog service returned {(int)response.StatusCode}.");
        }

        var product = await response.Content.ReadFromJsonAsync<CatalogProductResponse>(
            SerializerOptions,
            cancellationToken);

        if (product is null)
        {
            throw new BusinessRuleException("Catalog service returned an empty product snapshot.");
        }

        return MapToSnapshot(product);
    }

    private string BuildProductSnapshotPath(Guid productId)
    {
        if (string.IsNullOrWhiteSpace(_options.ProductSnapshotPath))
        {
            throw new InvalidOperationException("CatalogClient:ProductSnapshotPath is not configured.");
        }

        return _options.ProductSnapshotPath.Replace(
            "{productId}",
            productId.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static ProductSnapshot MapToSnapshot(CatalogProductResponse product)
    {
        return new ProductSnapshot(
            product.Id,
            product.Name,
            product.ImageUrl,
            product.Price,
            IsProductActive(product));
    }

    private static bool IsProductActive(CatalogProductResponse product)
    {
        if (product.IsActive.HasValue)
        {
            return product.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(product.Status))
        {
            return string.Equals(product.Status, "Active", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private sealed record CatalogProductResponse(
        Guid Id,
        string Name,
        string? Description,
        decimal Price,
        string? ImageUrl,
        string? Status,
        bool? IsActive);
}