using MediatR;

namespace ECommerce.Catalog.Application.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest;
