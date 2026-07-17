using Cart.Domain.Entities;

namespace Cart.Domain.Interfaces;

public interface ICartRepository
{
    Task<ShoppingCart?> GetByUserIdAsync(string userId);
    Task<ShoppingCart> GetOrCreateForUpdateAsync(string userId);
    Task SaveChangesAsync();
    Task ClearAsync(string userId);
}
