using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using OrderProcessing.Worker.Contracts;
using OrderProcessing.Worker.Messaging;

namespace OrderProcessing.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly OrderPlacedHandler _handler;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;

    public Worker(ILogger<Worker> logger, OrderPlacedHandler handler, IOptions<ServiceBusSettings> options)
    {
        _logger = logger;
        _handler = handler;
        _client = new ServiceBusClient(options.Value.ConnectionString);
        _processor = _client.CreateProcessor(options.Value.QueueName, new ServiceBusProcessorOptions());
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected on shutdown.
        }

        await _processor.StopProcessingAsync(CancellationToken.None);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var orderPlacedEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(args.Message.Body.ToString());
        if (orderPlacedEvent is not null)
            await _handler.HandleAsync(orderPlacedEvent);

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing Service Bus message from {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
