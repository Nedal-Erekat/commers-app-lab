using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Order.Application.Interfaces;

namespace Order.Infrastructure.Messaging;

public class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusEventPublisher(IOptions<ServiceBusSettings> options)
    {
        _client = new ServiceBusClient(options.Value.ConnectionString);
        _sender = _client.CreateSender(options.Value.QueueName);
    }

    public async Task PublishOrderPlacedAsync(OrderPlacedEvent orderPlacedEvent)
    {
        var message = new ServiceBusMessage(JsonSerializer.Serialize(orderPlacedEvent))
        {
            ContentType = "application/json",
            Subject = "OrderPlaced"
        };

        await _sender.SendMessageAsync(message);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
