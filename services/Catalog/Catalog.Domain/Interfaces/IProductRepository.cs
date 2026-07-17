using Catalog.Domain.Entities;

namespace Catalog.Domain.Interfaces;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Data, int TotalCount, string Source)> GetPagedAsync(int page, int pageSize, string? category = null);
    Task<IReadOnlyList<Product>> SearchByNameAsync(string term);
    Task<Product?> GetByIdAsync(int id);
    Task<bool> DecrementStockAsync(int id, int quantity);
}
