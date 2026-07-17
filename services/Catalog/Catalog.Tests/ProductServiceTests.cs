using Catalog.Application.DTOs;
using Catalog.Application.Services;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using Moq;

namespace Catalog.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repoMock = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repoMock.Object);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(1,   10,  1)]
    public async Task GetProductsAsync_CalculatesTotalPages_WithCeilingDivision(
        int totalCount, int pageSize, int expectedTotalPages)
    {
        _repoMock.Setup(r => r.GetPagedAsync(1, pageSize))
            .ReturnsAsync((new List<Product>(), totalCount, "Database"));

        var result = await _sut.GetProductsAsync(1, pageSize);

        Assert.Equal(expectedTotalPages, result.TotalPages);
    }

    [Fact]
    public async Task SearchProductsAsync_MapsProductsToDto()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Widget", Category = "Tools", Price = 9.99m, CreatedAt = new DateTime(2024, 1, 15) }
        };
        _repoMock.Setup(r => r.SearchByNameAsync("widget")).ReturnsAsync(products);

        var result = await _sut.SearchProductsAsync("widget");

        var dto = Assert.Single(result);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal("Tools", dto.Category);
        Assert.Equal(9.99m, dto.Price);
        Assert.Equal(new DateTime(2024, 1, 15), dto.CreatedAt);
    }

    [Fact]
    public async Task SearchProductsAsync_ReturnsEmptyList_WhenNoMatches()
    {
        _repoMock.Setup(r => r.SearchByNameAsync("xyz")).ReturnsAsync(new List<Product>());

        var result = await _sut.SearchProductsAsync("xyz");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsDto_WhenFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Product { Id = 1, Name = "Widget", Category = "Tools", Price = 9.99m, CreatedAt = new DateTime(2024, 1, 15) });

        var result = await _sut.GetProductAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Widget", result!.Name);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var result = await _sut.GetProductAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task DecrementStockAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.DecrementStockAsync(1, 2)).ReturnsAsync(true);

        var result = await _sut.DecrementStockAsync(1, 2);

        Assert.True(result);
        _repoMock.Verify(r => r.DecrementStockAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task GetProductsAsync_ForwardsCategoryToRepository()
    {
        _repoMock.Setup(r => r.GetPagedAsync(1, 10, "Tools"))
            .ReturnsAsync((new List<Product>(), 0, "Database"));

        await _sut.GetProductsAsync(1, 10, "Tools");

        _repoMock.Verify(r => r.GetPagedAsync(1, 10, "Tools"), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_DelegatesToRepository_AndReturnsDto()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => { p.Id = 1; p.CreatedAt = new DateTime(2024, 1, 15); return p; });

        var result = await _sut.CreateProductAsync(new CreateProductRequest("Widget", "Tools", 9.99m, 5));

        Assert.Equal(1, result.Id);
        Assert.Equal("Widget", result.Name);
        Assert.Equal(5, result.Stock);
        _repoMock.Verify(r => r.CreateAsync(It.Is<Product>(p => p.Name == "Widget" && p.Category == "Tools")), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ReturnsNull_WhenProductNotFound()
    {
        _repoMock.Setup(r => r.UpdateAsync(99, It.IsAny<Product>())).ReturnsAsync((Product?)null);

        var result = await _sut.UpdateProductAsync(99, new UpdateProductRequest("Widget", "Tools", 9.99m, 5));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProductAsync_ReturnsDto_WhenFound()
    {
        _repoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Product>()))
            .ReturnsAsync(new Product { Id = 1, Name = "Widget", Category = "Tools", Price = 12.5m, Stock = 3, CreatedAt = new DateTime(2024, 1, 15) });

        var result = await _sut.UpdateProductAsync(1, new UpdateProductRequest("Widget", "Tools", 12.5m, 3));

        Assert.NotNull(result);
        Assert.Equal(12.5m, result!.Price);
    }

    [Fact]
    public async Task DeleteProductAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _sut.DeleteProductAsync(1);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}
