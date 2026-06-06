using ECommerce.Catalog.Domain.Responses;
using MediatR;

namespace ECommerce.Catalog.Application.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse>;
