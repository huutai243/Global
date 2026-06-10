using Azure.Messaging.ServiceBus;
using ECommerce.Inventory.Application.ReserveInventory;
using ECommerce.Inventory.Worker.Options;
using ECommerce.Shared.Contracts;
using ECommerce.Shared.Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ECommerce.Inventory.Worker;

public sealed class ReserveInventoryConsumer(
    IServiceScopeFactory serviceScopeFactory,
    ServiceBusClient serviceBusClient,
    IOptions<ReserveInventoryConsumerOptions> options,
    ILogger<ReserveInventoryConsumer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ReserveInventoryConsumerOptions _options = options.Value;
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = serviceBusClient.CreateProcessor(_options.QueueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = _options.MaxConcurrentCalls
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        logger.LogInformation(
            "Reserve inventory consumer started. QueueName: {QueueName}",
            _options.QueueName);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var handler = scope.ServiceProvider.GetRequiredService<ReserveInventoryCommandHandler>();
        var payload = args.Message.Body.ToString();

        var command = JsonSerializer.Deserialize<ReserveInventoryCommand>(
            payload,
            SerializerOptions);

        if (command is null)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                "InvalidPayload",
                "ReserveInventoryCommand payload could not be deserialized.",
                args.CancellationToken);

            return;
        }

        try
        {
            await handler.HandleAsync(command, CreateMetadata(args.Message), payload, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Reserve inventory consumer failed. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                args.Message.MessageId,
                args.Message.CorrelationId);

            await args.AbandonMessageAsync(
                args.Message,
                cancellationToken: args.CancellationToken);
        }
    }

    private static MessageMetadata CreateMetadata(ServiceBusReceivedMessage message)
    {
        var causationId = message.ApplicationProperties.TryGetValue("CausationId", out var value)
            ? value?.ToString() ?? message.MessageId
            : message.MessageId;

        return new MessageMetadata(
            message.MessageId,
            message.CorrelationId ?? message.MessageId,
            causationId,
            message.EnqueuedTime.UtcDateTime);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Azure Service Bus consumer error. EntityPath: {EntityPath}, ErrorSource: {ErrorSource}",
            args.EntityPath,
            args.ErrorSource);

        return Task.CompletedTask;
    }
}