using Catalog.Domain.Entities;
using Catalog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Product> Data, int TotalCount, string Source)> GetPagedAsync(int page, int pageSize, string? category = null)
    {
        var query = _context.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var totalCount = await query.CountAsync();
        var data = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount, "Database");
    }

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(string term)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.StartsWith(term))
            .OrderBy(p => p.Name)
            .Take(20)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> DecrementStockAsync(int id, int quantity)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null || product.Stock < quantity)
            return false;

        product.Stock -= quantity;
        await _context.SaveChangesAsync();
        return true;
    }
}
