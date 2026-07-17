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

    public async Task<(IReadOnlyList<Product> Data, int TotalCount, string Source)> GetPagedAsync(int page, int pageSize)
    {
        var totalCount = await _context.Products.CountAsync();
        var data = await _context.Products
            .AsNoTracking()
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
}
