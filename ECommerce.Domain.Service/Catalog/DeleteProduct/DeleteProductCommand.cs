using MediatR;

namespace ECommerce.Domain.Service.Catalog.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest;
