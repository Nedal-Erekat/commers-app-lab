using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Mcp.Server;

[McpServerToolType]
public class CommerceTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly CommerceService _service;

    public CommerceTools(CommerceService service)
    {
        _service = service;
    }

    [McpServerTool(Name = "search-products")]
    [Description("Search the commerce catalog by product name.")]
    public async Task<string> SearchProducts(
        [Description("Search term, matched as a name prefix (e.g. 'wid' matches 'Widget').")] string query)
    {
        var results = await _service.SearchProductsAsync(query);
        return JsonSerializer.Serialize(results, JsonOptions);
    }

    [McpServerTool(Name = "recommend-products")]
    [Description("Recommend a handful of products, optionally narrowed to one category.")]
    public async Task<string> RecommendProducts(
        [Description("Exact category name to filter by (e.g. 'Electronics'). Omit for general recommendations.")] string? category = null,
        [Description("How many products to recommend, 1-20.")] int take = 5)
    {
        var results = await _service.RecommendProductsAsync(category, take);
        return JsonSerializer.Serialize(results, JsonOptions);
    }

    [McpServerTool(Name = "get-order-status")]
    [Description("Look up the status and contents of a customer's order. Requires a JWT for that customer.")]
    public async Task<string> GetOrderStatus(
        [Description("JWT bearer token for the customer who placed the order, obtained from POST /api/auth/login.")] string bearerToken,
        [Description("The order's ID (GUID).")] string orderId)
    {
        var order = await _service.GetOrderStatusAsync(bearerToken, orderId);
        return order is null ? "Order not found." : JsonSerializer.Serialize(order, JsonOptions);
    }

    [McpServerTool(Name = "add-to-cart")]
    [Description("Add a product to a customer's cart. Requires a JWT for that customer.")]
    public async Task<string> AddToCart(
        [Description("JWT bearer token for the customer, obtained from POST /api/auth/login.")] string bearerToken,
        [Description("The product's ID.")] int productId,
        [Description("Quantity to add.")] int quantity = 1)
    {
        var cart = await _service.AddToCartAsync(bearerToken, productId, quantity);
        return cart is null ? "Could not add that product to the cart (not found)." : JsonSerializer.Serialize(cart, JsonOptions);
    }
}
