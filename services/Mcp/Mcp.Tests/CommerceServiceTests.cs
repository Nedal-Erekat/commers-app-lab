using Mcp.Server;
using Mcp.Server.Contracts;
using Moq;

namespace Mcp.Tests;

public class CommerceServiceTests
{
    private readonly Mock<ICommerceClient> _clientMock = new();
    private readonly CommerceService _sut;

    public CommerceServiceTests()
    {
        _sut = new CommerceService(_clientMock.Object);
    }

    [Fact]
    public async Task SearchProductsAsync_TrimsQuery_AndDelegates()
    {
        _clientMock.Setup(c => c.SearchProductsAsync("widget")).ReturnsAsync([]);

        await _sut.SearchProductsAsync("  widget  ");

        _clientMock.Verify(c => c.SearchProductsAsync("widget"), Times.Once);
    }

    [Fact]
    public async Task SearchProductsAsync_ThrowsArgumentException_WhenQueryBlank()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SearchProductsAsync("   "));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 5)]
    [InlineData(50, 20)]
    public async Task RecommendProductsAsync_ClampsTakeTo1Through20(int requestedTake, int expectedTake)
    {
        _clientMock.Setup(c => c.GetProductsByCategoryAsync(It.IsAny<string?>(), It.IsAny<int>())).ReturnsAsync([]);

        await _sut.RecommendProductsAsync("Tools", requestedTake);

        _clientMock.Verify(c => c.GetProductsByCategoryAsync("Tools", expectedTake), Times.Once);
    }

    [Fact]
    public async Task GetOrderStatusAsync_ThrowsArgumentException_WhenBearerTokenBlank()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetOrderStatusAsync("", "order-1"));
    }

    [Fact]
    public async Task GetOrderStatusAsync_Delegates_WhenValid()
    {
        var order = new OrderSummary("order-1", "Placed", 19.98m, DateTime.UtcNow, []);
        _clientMock.Setup(c => c.GetOrderAsync("token", "order-1")).ReturnsAsync(order);

        var result = await _sut.GetOrderStatusAsync("token", "order-1");

        Assert.Equal(order, result);
    }

    [Fact]
    public async Task AddToCartAsync_ThrowsArgumentException_WhenQuantityLessThanOne()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddToCartAsync("token", 1, 0));
    }

    [Fact]
    public async Task AddToCartAsync_ThrowsArgumentException_WhenBearerTokenBlank()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.AddToCartAsync("", 1, 1));
    }

    [Fact]
    public async Task AddToCartAsync_Delegates_WhenValid()
    {
        var cart = new CartSummary([new CartLineSummary(1, "Widget", 9.99m, 2)], 19.98m);
        _clientMock.Setup(c => c.AddToCartAsync("token", 1, 2)).ReturnsAsync(cart);

        var result = await _sut.AddToCartAsync("token", 1, 2);

        Assert.Equal(cart, result);
    }
}
