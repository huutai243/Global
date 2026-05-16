using ECommerce.Domain.Core.Catalog.Responses;
using MediatR;

namespace ECommerce.Domain.Service.Catalog.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductResponse>;
