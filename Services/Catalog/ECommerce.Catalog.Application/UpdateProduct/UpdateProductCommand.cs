using ECommerce.Catalog.Domain.Models;
using ECommerce.Catalog.Domain.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Catalog.Application.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    ProductStatus Status,
    FileUploadRequest? Image) : IRequest<ProductResponse>;
