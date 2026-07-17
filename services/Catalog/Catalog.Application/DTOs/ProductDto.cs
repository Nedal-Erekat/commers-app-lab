namespace Catalog.Application.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string Category,
    decimal Price,
    DateTime CreatedAt);
