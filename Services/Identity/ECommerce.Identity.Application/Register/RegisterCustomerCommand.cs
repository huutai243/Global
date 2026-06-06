using ECommerce.Identity.Application.Shared;
using MediatR;

namespace ECommerce.Identity.Application.Register;

public sealed record RegisterCustomerCommand(string Email, string Password, string FullName) : IRequest<AuthResponse>;
