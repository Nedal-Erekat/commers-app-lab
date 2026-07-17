using System.Net.Http.Headers;
using System.Net.Http.Json;
using Order.Application.Interfaces;

namespace Order.Infrastructure.Clients;

public class CartClient : ICartClient
{
    private readonly HttpClient _http;

    public CartClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<CartSnapshot?> GetCartAsync(string bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var dto = await response.Content.ReadFromJsonAsync<CartResponse>();
        if (dto is null) return null;

        var items = dto.Items.Select(i => new CartSnapshotItem(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList();
        return new CartSnapshot(items, dto.Total);
    }

    public async Task ClearCartAsync(string bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/cart");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        await _http.SendAsync(request);
    }

    private record CartItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
    private record CartResponse(List<CartItemResponse> Items, decimal Total);
}
