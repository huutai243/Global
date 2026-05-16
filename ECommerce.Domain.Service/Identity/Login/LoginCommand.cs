using ECommerce.Domain.Service.Identity.Shared;
using MediatR;

namespace ECommerce.Domain.Service.Identity.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
