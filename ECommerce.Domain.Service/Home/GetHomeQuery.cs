using MediatR;

namespace ECommerce.Domain.Service.Home.GetHome;

public sealed record GetHomeQuery : IRequest<HomeResponse>;