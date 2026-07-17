using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessing.Worker;
using OrderProcessing.Worker.Contracts;
using OrderProcessing.Worker.Inventory;

namespace OrderProcessing.Tests;

public class OrderPlacedHandlerTests
{
    private readonly Mock<IInventoryClient> _inventoryClientMock = new();
    private readonly OrderPlacedHandler _sut;

    public OrderPlacedHandlerTests()
    {
        _sut = new OrderPlacedHandler(_inventoryClientMock.Object, new Mock<ILogger<OrderPlacedHandler>>().Object);
    }

    [Fact]
    public async Task HandleAsync_DecrementsStockForEveryItem()
    {
        var orderPlacedEvent = new OrderPlacedEvent(
            Guid.NewGuid(),
            "user-1",
            [new OrderPlacedItem(1, "Widget", 2), new OrderPlacedItem(2, "Gadget", 1)],
            DateTime.UtcNow);

        _inventoryClientMock.Setup(c => c.DecrementStockAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

        await _sut.HandleAsync(orderPlacedEvent);

        _inventoryClientMock.Verify(c => c.DecrementStockAsync(1, 2), Times.Once);
        _inventoryClientMock.Verify(c => c.DecrementStockAsync(2, 1), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ContinuesProcessingRemainingItems_WhenOneDecrementFails()
    {
        var orderPlacedEvent = new OrderPlacedEvent(
            Guid.NewGuid(),
            "user-1",
            [new OrderPlacedItem(1, "Widget", 2), new OrderPlacedItem(2, "Gadget", 1)],
            DateTime.UtcNow);

        _inventoryClientMock.Setup(c => c.DecrementStockAsync(1, 2)).ReturnsAsync(false);
        _inventoryClientMock.Setup(c => c.DecrementStockAsync(2, 1)).ReturnsAsync(true);

        await _sut.HandleAsync(orderPlacedEvent);

        _inventoryClientMock.Verify(c => c.DecrementStockAsync(2, 1), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNothing_WhenOrderHasNoItems()
    {
        var orderPlacedEvent = new OrderPlacedEvent(Guid.NewGuid(), "user-1", [], DateTime.UtcNow);

        await _sut.HandleAsync(orderPlacedEvent);

        _inventoryClientMock.Verify(c => c.DecrementStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
