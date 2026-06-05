namespace ECommerce.Infrastructure.Security.Core;

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken);
}