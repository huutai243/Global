using ECommerce.Domain.Service.Identity.Shared;
using MediatR;

namespace ECommerce.Domain.Service.Identity.Profile
{
    public sealed record GetProfileQuery : IRequest<ProfileResponse>;
}
