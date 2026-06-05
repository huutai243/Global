using MediatR;

namespace ECommerce.Domain.Service.Identity.ForgotPassword
{
    public sealed record ForgotPasswordCommand(string Email) : IRequest;
}
