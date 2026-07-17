using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;

namespace Catalog.Application.Services;

public class ProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<Product>> GetProductsAsync(int page, int pageSize, string? category = null)
    {
        var (data, totalCount, source) = await _repository.GetPagedAsync(page, pageSize, category);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<Product>(data, page, pageSize, totalCount, totalPages, source);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string term)
    {
        var products = await _repository.SearchByNameAsync(term);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Category, p.Price, p.Stock, p.CreatedAt)).ToList();
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product is null ? null : new ProductDto(product.Id, product.Name, product.Category, product.Price, product.Stock, product.CreatedAt);
    }

    public Task<bool> DecrementStockAsync(int id, int quantity) => _repository.DecrementStockAsync(id, quantity);

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Category = request.Category,
            Price = request.Price,
            Stock = request.Stock
        };
        var created = await _repository.CreateAsync(product);
        return new ProductDto(created.Id, created.Name, created.Category, created.Price, created.Stock, created.CreatedAt);
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Category = request.Category,
            Price = request.Price,
            Stock = request.Stock
        };
        var updated = await _repository.UpdateAsync(id, product);
        return updated is null ? null : new ProductDto(updated.Id, updated.Name, updated.Category, updated.Price, updated.Stock, updated.CreatedAt);
    }

    public Task<bool> DeleteProductAsync(int id) => _repository.DeleteAsync(id);
}
