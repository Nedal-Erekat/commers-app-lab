namespace Cart.Application.Interfaces;

public record ProductInfo(int Id, string Name, decimal Price);

public interface IProductCatalogClient
{
    Task<ProductInfo?> GetProductAsync(int productId);
}
