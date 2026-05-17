using ECommerce.Domain.Service.Identity.Shared;
using MediatR;

namespace ECommerce.Domain.Service.Identity.Profile;

public sealed record UpdateProfileCommand(string FullName) : IRequest<ProfileResponse>;