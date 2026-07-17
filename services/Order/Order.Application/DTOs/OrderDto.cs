namespace Order.Application.DTOs;

public record OrderDto(Guid Id, string Status, decimal TotalAmount, DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items);
