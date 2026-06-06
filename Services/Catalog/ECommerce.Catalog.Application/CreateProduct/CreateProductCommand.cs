using ECommerce.Catalog.Domain.Responses;
using ECommerce.Infrastructure.Storage;
using MediatR;

namespace ECommerce.Catalog.Application.CreateProduct;

public sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int InitialStock,
    FileUploadRequest? Image) : IRequest<ProductResponse>;
