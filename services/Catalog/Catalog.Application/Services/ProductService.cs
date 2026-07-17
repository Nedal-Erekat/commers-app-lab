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

    public async Task<PagedResult<Product>> GetProductsAsync(int page, int pageSize)
    {
        var (data, totalCount, source) = await _repository.GetPagedAsync(page, pageSize);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<Product>(data, page, pageSize, totalCount, totalPages, source);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string term)
    {
        var products = await _repository.SearchByNameAsync(term);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Category, p.Price, p.CreatedAt)).ToList();
    }
}
