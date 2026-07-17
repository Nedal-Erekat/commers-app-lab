namespace Cart.Application.DTOs;

public record CartItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
