using Mcp.Server.Contracts;

namespace Mcp.Server;

public class CommerceService
{
    private readonly ICommerceClient _client;

    public CommerceService(ICommerceClient client)
    {
        _client = client;
    }

    public Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("query is required.", nameof(query));

        return _client.SearchProductsAsync(query.Trim());
    }

    public Task<IReadOnlyList<ProductSummary>> RecommendProductsAsync(string? category, int take = 5)
    {
        var clampedTake = Math.Clamp(take, 1, 20);
        return _client.GetProductsByCategoryAsync(category, clampedTake);
    }

    public Task<OrderSummary?> GetOrderStatusAsync(string bearerToken, string orderId)
    {
        RequireBearerToken(bearerToken);
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("orderId is required.", nameof(orderId));

        return _client.GetOrderAsync(bearerToken, orderId);
    }

    public Task<CartSummary?> AddToCartAsync(string bearerToken, int productId, int quantity)
    {
        RequireBearerToken(bearerToken);
        if (quantity < 1)
            throw new ArgumentException("quantity must be at least 1.", nameof(quantity));

        return _client.AddToCartAsync(bearerToken, productId, quantity);
    }

    private static void RequireBearerToken(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            throw new ArgumentException("bearerToken is required — obtain one from POST /api/auth/login first.", nameof(bearerToken));
    }
}
