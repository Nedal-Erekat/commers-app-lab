using Microsoft.Extensions.Logging;
using OrderProcessing.Worker.Contracts;
using OrderProcessing.Worker.Inventory;

namespace OrderProcessing.Worker;

public class OrderPlacedHandler
{
    private readonly IInventoryClient _inventoryClient;
    private readonly ILogger<OrderPlacedHandler> _logger;

    public OrderPlacedHandler(IInventoryClient inventoryClient, ILogger<OrderPlacedHandler> logger)
    {
        _inventoryClient = inventoryClient;
        _logger = logger;
    }

    // A failed stock decrement (insufficient stock, product gone) is a business
    // outcome, not a transient fault — retrying the message wouldn't help, so we
    // log and move on rather than let it dead-letter the whole order.
    public async Task HandleAsync(OrderPlacedEvent orderPlacedEvent)
    {
        _logger.LogInformation(
            "Processing OrderPlaced {OrderId} for user {UserId} with {ItemCount} item(s)",
            orderPlacedEvent.OrderId, orderPlacedEvent.UserId, orderPlacedEvent.Items.Count);

        foreach (var item in orderPlacedEvent.Items)
        {
            var success = await _inventoryClient.DecrementStockAsync(item.ProductId, item.Quantity);

            if (success)
                _logger.LogInformation("Decremented stock for product {ProductId} by {Quantity}", item.ProductId, item.Quantity);
            else
                _logger.LogWarning("Could not decrement stock for product {ProductId} (order {OrderId})", item.ProductId, orderPlacedEvent.OrderId);
        }

        _logger.LogInformation(
            "[Notification] Order confirmation would be emailed to user {UserId} for order {OrderId}",
            orderPlacedEvent.UserId, orderPlacedEvent.OrderId);
    }
}
