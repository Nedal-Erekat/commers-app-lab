using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using Catalog.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace Catalog.Tests;

public class CachedProductRepositoryTests
{
    private readonly Mock<IProductRepository> _innerMock = new();
    private readonly IDistributedCache _cache;
    private readonly CachedProductRepository _sut;

    public CachedProductRepositoryTests()
    {
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _sut = new CachedProductRepository(_innerMock.Object, _cache);
    }

    [Fact]
    public async Task GetPagedAsync_OnCacheMiss_CallsInnerAndReturnsDatabase()
    {
        var products = new List<Product> { new() { Id = 1, Name = "Hammer" } };
        _innerMock.Setup(r => r.GetPagedAsync(1, 10)).ReturnsAsync((products, 1, "Database"));

        var result = await _sut.GetPagedAsync(1, 10);

        Assert.Equal("Database", result.Source);
        _innerMock.Verify(r => r.GetPagedAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_OnCacheHit_ReturnsCache_WithoutCallingInnerAgain()
    {
        var products = new List<Product> { new() { Id = 1, Name = "Hammer" } };
        _innerMock.Setup(r => r.GetPagedAsync(1, 10)).ReturnsAsync((products, 1, "Database"));

        await _sut.GetPagedAsync(1, 10);         // populates cache
        var result = await _sut.GetPagedAsync(1, 10);  // should hit cache

        Assert.Equal("Cache", result.Source);
        _innerMock.Verify(r => r.GetPagedAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_UsesSeparateCacheKeys_PerPageParams()
    {
        _innerMock.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<Product>(), 0, "Database"));

        await _sut.GetPagedAsync(1, 10);
        await _sut.GetPagedAsync(2, 10);

        _innerMock.Verify(r => r.GetPagedAsync(1, 10), Times.Once);
        _innerMock.Verify(r => r.GetPagedAsync(2, 10), Times.Once);
    }

    [Fact]
    public async Task SearchByNameAsync_BypassesCache_AlwaysCallsInner()
    {
        _innerMock.Setup(r => r.SearchByNameAsync("drill")).ReturnsAsync(new List<Product>());

        await _sut.SearchByNameAsync("drill");
        await _sut.SearchByNameAsync("drill");

        _innerMock.Verify(r => r.SearchByNameAsync("drill"), Times.Exactly(2));
    }
}
