using ECommerce.Domain.Core.Catalog.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.CreateProduct;

public sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal Price,
    int InitialStock) : IRequest<ProductResponse>;
