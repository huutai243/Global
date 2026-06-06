using MediatR;

namespace ECommerce.Catalog.Application.Home;

public sealed record GetHomeQuery : IRequest<HomeResponse>;
