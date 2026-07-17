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
}
