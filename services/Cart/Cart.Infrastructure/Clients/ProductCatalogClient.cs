using System.Net.Http.Json;
using Cart.Application.Interfaces;

namespace Cart.Infrastructure.Clients;

public class ProductCatalogClient : IProductCatalogClient
{
    private readonly HttpClient _http;

    public ProductCatalogClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProductInfo?> GetProductAsync(int productId)
    {
        var response = await _http.GetAsync($"/api/products/{productId}");
        if (!response.IsSuccessStatusCode) return null;

        var dto = await response.Content.ReadFromJsonAsync<CatalogProductResponse>();
        return dto is null ? null : new ProductInfo(dto.Id, dto.Name, dto.Price);
    }

    private record CatalogProductResponse(int Id, string Name, string Category, decimal Price, DateTime CreatedAt);
}
