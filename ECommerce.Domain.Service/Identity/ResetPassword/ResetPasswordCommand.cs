using MediatR;

namespace ECommerce.Domain.Service.Identity.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest;