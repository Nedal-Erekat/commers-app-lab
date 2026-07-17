using System.Net.Http.Headers;
using System.Net.Http.Json;
using Mcp.Server.Contracts;

namespace Mcp.Server;

public class GatewayCommerceClient : ICommerceClient
{
    private readonly HttpClient _http;

    public GatewayCommerceClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<ProductSummary>> SearchProductsAsync(string query)
    {
        var products = await _http.GetFromJsonAsync<List<ProductWire>>($"/api/products/search?q={Uri.EscapeDataString(query)}");
        return (products ?? []).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ProductSummary>> GetProductsByCategoryAsync(string? category, int take)
    {
        var url = $"/api/products?page=1&pageSize={take}";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"&category={Uri.EscapeDataString(category)}";

        var page = await _http.GetFromJsonAsync<ProductPageWire>(url);
        return (page?.Data ?? []).Select(Map).ToList();
    }

    public async Task<OrderSummary?> GetOrderAsync(string bearerToken, string orderId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{orderId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var order = await response.Content.ReadFromJsonAsync<OrderWire>();
        return order is null ? null : Map(order);
    }

    public async Task<CartSummary?> AddToCartAsync(string bearerToken, int productId, int quantity)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/items")
        {
            Content = JsonContent.Create(new { productId, quantity })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var cart = await response.Content.ReadFromJsonAsync<CartWire>();
        return cart is null ? null : Map(cart);
    }

    private static ProductSummary Map(ProductWire p) => new(p.Id, p.Name, p.Category, p.Price, p.Stock);

    private static OrderSummary Map(OrderWire o) => new(
        o.Id, o.Status, o.TotalAmount, o.CreatedAt,
        o.Items.Select(i => new OrderLineSummary(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList());

    private static CartSummary Map(CartWire c) => new(
        c.Items.Select(i => new CartLineSummary(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList(), c.Total);

    private record ProductWire(int Id, string Name, string Category, decimal Price, int Stock, DateTime CreatedAt);
    private record ProductPageWire(List<ProductWire> Data);
    private record CartItemWire(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
    private record CartWire(List<CartItemWire> Items, decimal Total);
    private record OrderItemWire(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
    private record OrderWire(string Id, string Status, decimal TotalAmount, DateTime CreatedAt, List<OrderItemWire> Items);
}
