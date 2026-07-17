using Cart.Domain.Entities;
using Cart.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ShoppingCart?> GetByUserIdAsync(string userId)
    {
        return await _context.ShoppingCarts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<ShoppingCart> GetOrCreateForUpdateAsync(string userId)
    {
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = userId };
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task ClearAsync(string userId)
    {
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is not null)
        {
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();
        }
    }
}
