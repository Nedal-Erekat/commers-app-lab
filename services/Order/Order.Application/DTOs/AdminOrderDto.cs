namespace Order.Application.DTOs;

public record AdminOrderDto(Guid Id, string UserId, string Status, decimal TotalAmount, DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items);
