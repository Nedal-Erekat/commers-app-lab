namespace Order.Application.DTOs;

public record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
