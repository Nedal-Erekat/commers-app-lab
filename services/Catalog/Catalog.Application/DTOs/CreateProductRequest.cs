namespace Catalog.Application.DTOs;

public record CreateProductRequest(string Name, string Category, decimal Price, int Stock);
