using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using Order.Domain.Interfaces;

namespace Order.Infrastructure.Persistence;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CustomerOrder order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CustomerOrder>> GetByUserIdAsync(string userId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<CustomerOrder?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}
