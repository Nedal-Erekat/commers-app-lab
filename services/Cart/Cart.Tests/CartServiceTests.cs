using Cart.Application.Interfaces;
using Cart.Application.Services;
using Cart.Domain.Entities;
using Cart.Domain.Interfaces;
using Moq;

namespace Cart.Tests;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _repoMock = new();
    private readonly Mock<IProductCatalogClient> _catalogMock = new();
    private readonly CartService _sut;

    public CartServiceTests()
    {
        _sut = new CartService(_repoMock.Object, _catalogMock.Object);
    }

    [Fact]
    public async Task AddItemAsync_ReturnsNull_WhenProductNotFound()
    {
        _catalogMock.Setup(c => c.GetProductAsync(99)).ReturnsAsync((ProductInfo?)null);

        var result = await _sut.AddItemAsync("user-1", 99, 1);

        Assert.Null(result);
        _repoMock.Verify(r => r.GetOrCreateForUpdateAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_AddsNewItem_WhenProductFound()
    {
        _catalogMock.Setup(c => c.GetProductAsync(1)).ReturnsAsync(new ProductInfo(1, "Widget", 9.99m));
        var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = "user-1" };
        _repoMock.Setup(r => r.GetOrCreateForUpdateAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.AddItemAsync("user-1", 1, 2);

        Assert.NotNull(result);
        var item = Assert.Single(result!.Items);
        Assert.Equal(1, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(19.98m, result.Total);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_IncrementsQuantity_WhenItemAlreadyInCart()
    {
        _catalogMock.Setup(c => c.GetProductAsync(1)).ReturnsAsync(new ProductInfo(1, "Widget", 9.99m));
        var cart = new ShoppingCart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = [new CartItem { Id = Guid.NewGuid(), ProductId = 1, ProductName = "Widget", UnitPrice = 9.99m, Quantity = 1 }]
        };
        _repoMock.Setup(r => r.GetOrCreateForUpdateAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.AddItemAsync("user-1", 1, 2);

        var item = Assert.Single(result!.Items);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public async Task GetCartAsync_ReturnsEmptyCart_WhenNoCartExists()
    {
        _repoMock.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync((ShoppingCart?)null);

        var result = await _sut.GetCartAsync("user-1");

        Assert.Empty(result.Items);
        Assert.Equal(0m, result.Total);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesMatchingItem()
    {
        var cart = new ShoppingCart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items =
            [
                new CartItem { Id = Guid.NewGuid(), ProductId = 1, ProductName = "Widget", UnitPrice = 9.99m, Quantity = 1 },
                new CartItem { Id = Guid.NewGuid(), ProductId = 2, ProductName = "Gadget", UnitPrice = 5.00m, Quantity = 1 }
            ]
        };
        _repoMock.Setup(r => r.GetOrCreateForUpdateAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.RemoveItemAsync("user-1", 1);

        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.ProductId);
    }
}
