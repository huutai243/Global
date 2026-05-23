using ECommerce.Domain.Core.Catalog.Models;
using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    ProductStatus Status,
    FileUploadRequest? Image) : IRequest<ProductResponse>;
