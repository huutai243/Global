using System.Text;
using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ECommerce.Infrastructure.RabbitMq;

public sealed class RabbitMqPublisher(IOptions<RabbitMqSettings> options, ILogger<RabbitMqPublisher> logger) : IRabbitMqPublisher
{
    public Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Topic, durable: true);

        var body = Encoding.UTF8.GetBytes(payload);
        channel.BasicPublish(settings.ExchangeName, eventType, basicProperties: null, body: body);
        logger.LogInformation("Outbox publish succeeded for {EventType}", eventType);
        return Task.CompletedTask;
    }
}
