using ECommerce.Domain.Core.Catalog.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.CreateProduct;

public sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int InitialStock,
    FileUploadRequest? Image) : IRequest<ProductResponse>;
