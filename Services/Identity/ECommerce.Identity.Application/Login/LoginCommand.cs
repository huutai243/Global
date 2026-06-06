using ECommerce.Identity.Application.Shared;
using MediatR;

namespace ECommerce.Identity.Application.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
