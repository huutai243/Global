using MediatR;

namespace ECommerce.Inventory.Application.GetProductAvailability;

public sealed record GetProductAvailabilityQuery(Guid ProductId)
    : IRequest<ProductAvailabilityResponse>;