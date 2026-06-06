using MediatR;

namespace ECommerce.Identity.Application.ForgotPassword
{
    public sealed record ForgotPasswordCommand(string Email) : IRequest;
}
