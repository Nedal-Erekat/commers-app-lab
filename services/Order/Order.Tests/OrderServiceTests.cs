using Moq;
using Order.Application.Interfaces;
using Order.Application.Services;
using Order.Domain.Entities;
using Order.Domain.Interfaces;

namespace Order.Tests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repoMock = new();
    private readonly Mock<ICartClient> _cartClientMock = new();
    private readonly Mock<IEventPublisher> _eventPublisherMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_repoMock.Object, _cartClientMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task CheckoutAsync_ReturnsNull_WhenCartIsEmpty()
    {
        _cartClientMock.Setup(c => c.GetCartAsync("token")).ReturnsAsync(new CartSnapshot([], 0m));

        var result = await _sut.CheckoutAsync("user-1", "token");

        Assert.Null(result);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<CustomerOrder>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_ReturnsNull_WhenCartUnreachable()
    {
        _cartClientMock.Setup(c => c.GetCartAsync("token")).ReturnsAsync((CartSnapshot?)null);

        var result = await _sut.CheckoutAsync("user-1", "token");

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckoutAsync_CreatesOrder_AndClearsCart_WhenCartHasItems()
    {
        var cartItems = new List<CartSnapshotItem> { new(1, "Widget", 9.99m, 2) };
        _cartClientMock.Setup(c => c.GetCartAsync("token")).ReturnsAsync(new CartSnapshot(cartItems, 19.98m));

        var result = await _sut.CheckoutAsync("user-1", "token");

        Assert.NotNull(result);
        Assert.Equal(19.98m, result!.TotalAmount);
        Assert.Equal("Placed", result.Status);
        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.ProductId);
        Assert.Equal(2, item.Quantity);

        _repoMock.Verify(r => r.AddAsync(It.Is<CustomerOrder>(o => o.UserId == "user-1" && o.Items.Count == 1)), Times.Once);
        _cartClientMock.Verify(c => c.ClearCartAsync("token"), Times.Once);
        _eventPublisherMock.Verify(p => p.PublishOrderPlacedAsync(It.Is<OrderPlacedEvent>(
            e => e.UserId == "user-1" && e.Items.Count == 1 && e.Items[0].ProductId == 1)), Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_DoesNotPublishEvent_WhenCartIsEmpty()
    {
        _cartClientMock.Setup(c => c.GetCartAsync("token")).ReturnsAsync(new CartSnapshot([], 0m));

        await _sut.CheckoutAsync("user-1", "token");

        _eventPublisherMock.Verify(p => p.PublishOrderPlacedAsync(It.IsAny<OrderPlacedEvent>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsNull_WhenOrderBelongsToDifferentUser()
    {
        var order = new CustomerOrder { Id = Guid.NewGuid(), UserId = "other-user", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.GetOrderAsync("user-1", order.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsOrder_WhenOwnedByRequester()
    {
        var order = new CustomerOrder { Id = Guid.NewGuid(), UserId = "user-1", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.GetOrderAsync("user-1", order.Id);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result!.Id);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ReturnsOrders_AcrossAllUsers()
    {
        var orders = new List<CustomerOrder>
        {
            new() { Id = Guid.NewGuid(), UserId = "user-1", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = "user-2", CreatedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(orders);

        var result = await _sut.GetAllOrdersAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.UserId == "user-1");
        Assert.Contains(result, o => o.UserId == "user-2");
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ThrowsArgumentException_ForUnknownStatus()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateOrderStatusAsync(Guid.NewGuid(), "Bogus"));
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ReturnsNull_WhenOrderNotFound()
    {
        _repoMock.Setup(r => r.UpdateStatusAsync(It.IsAny<Guid>(), "Shipped")).ReturnsAsync((CustomerOrder?)null);

        var result = await _sut.UpdateOrderStatusAsync(Guid.NewGuid(), "Shipped");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ReturnsUpdatedOrder_WhenValid()
    {
        var orderId = Guid.NewGuid();
        var order = new CustomerOrder { Id = orderId, UserId = "user-1", Status = "Shipped", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.UpdateStatusAsync(orderId, "Shipped")).ReturnsAsync(order);

        var result = await _sut.UpdateOrderStatusAsync(orderId, "Shipped");

        Assert.NotNull(result);
        Assert.Equal("Shipped", result!.Status);
    }
}
