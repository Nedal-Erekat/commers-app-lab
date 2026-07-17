using Mcp.Server.Contracts;

namespace Mcp.Server;

public interface ICommerceClient
{
    Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string query);
    Task<IReadOnlyList<ProductSummary>> GetProductsByCategoryAsync(string? category, int take);
    Task<OrderSummary?> GetOrderAsync(string bearerToken, string orderId);
    Task<CartSummary?> AddToCartAsync(string bearerToken, int productId, int quantity);
}
