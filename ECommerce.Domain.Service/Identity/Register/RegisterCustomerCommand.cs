using ECommerce.Domain.Service.Identity.Shared;
using MediatR;

namespace ECommerce.Domain.Service.Identity.Register;

public sealed record RegisterCustomerCommand(string Email, string Password, string FullName) : IRequest<AuthResponse>;
