namespace ECommerce.Infrastructure.Security.Core;

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string AppPassword { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
}