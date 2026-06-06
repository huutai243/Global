using ECommerce.Identity.Application.Shared;
using MediatR;

namespace ECommerce.Identity.Application.Profile;

public sealed record UpdateProfileCommand(string FullName) : IRequest<ProfileResponse>;