using System.Net.Http.Json;

namespace OrderProcessing.Worker.Inventory;

public class CatalogInventoryClient : IInventoryClient
{
    private readonly HttpClient _http;

    public CatalogInventoryClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> DecrementStockAsync(int productId, int quantity)
    {
        var response = await _http.PostAsJsonAsync($"/api/products/{productId}/decrement-stock", new { quantity });
        return response.IsSuccessStatusCode;
    }
}
