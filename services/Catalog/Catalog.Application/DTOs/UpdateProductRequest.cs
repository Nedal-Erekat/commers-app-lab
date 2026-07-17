namespace Catalog.Application.DTOs;

public record UpdateProductRequest(string Name, string Category, decimal Price, int Stock);
