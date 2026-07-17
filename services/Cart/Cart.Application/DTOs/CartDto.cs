namespace Cart.Application.DTOs;

public record CartDto(IReadOnlyList<CartItemDto> Items, decimal Total);
