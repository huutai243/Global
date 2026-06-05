using ECommerce.Infrastructure.Security.Core;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ECommerce.Infrastructure.Security;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;

    public SmtpEmailSender(IOptions<SmtpSettings> smtpOptions)
    {
        _smtpSettings = smtpOptions.Value;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            _smtpSettings.FromName,
            _smtpSettings.FromEmail));

        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = body
        }.ToMessageBody();

        using var smtpClient = new SmtpClient();

        await smtpClient.ConnectAsync(
            _smtpSettings.Host,
            _smtpSettings.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        await smtpClient.AuthenticateAsync(
            _smtpSettings.UserName,
            _smtpSettings.AppPassword,
            cancellationToken);

        await smtpClient.SendAsync(message, cancellationToken);

        await smtpClient.DisconnectAsync(
            quit: true,
            cancellationToken);
    }
}