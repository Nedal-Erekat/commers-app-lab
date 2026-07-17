using Cart.Application.DTOs;
using Cart.Application.Interfaces;
using Cart.Domain.Entities;
using Cart.Domain.Interfaces;

namespace Cart.Application.Services;

public class CartService
{
    private readonly ICartRepository _repository;
    private readonly IProductCatalogClient _catalogClient;

    public CartService(ICartRepository repository, IProductCatalogClient catalogClient)
    {
        _repository = repository;
        _catalogClient = catalogClient;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await _repository.GetByUserIdAsync(userId);
        return Map(cart);
    }

    public async Task<CartDto?> AddItemAsync(string userId, int productId, int quantity)
    {
        var product = await _catalogClient.GetProductAsync(productId);
        if (product is null) return null;

        var cart = await _repository.GetOrCreateForUpdateAsync(userId);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }

        await _repository.SaveChangesAsync();
        return Map(cart);
    }

    public async Task<CartDto> RemoveItemAsync(string userId, int productId)
    {
        var cart = await _repository.GetOrCreateForUpdateAsync(userId);
        cart.Items.RemoveAll(i => i.ProductId == productId);
        await _repository.SaveChangesAsync();
        return Map(cart);
    }

    public Task ClearCartAsync(string userId) => _repository.ClearAsync(userId);

    private static CartDto Map(ShoppingCart? cart)
    {
        var items = cart?.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList()
            ?? [];
        return new CartDto(items, items.Sum(i => i.UnitPrice * i.Quantity));
    }
}
