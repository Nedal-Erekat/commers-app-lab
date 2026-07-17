using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Domain.Interfaces;

namespace Order.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ICartClient _cartClient;

    public OrderService(IOrderRepository repository, ICartClient cartClient)
    {
        _repository = repository;
        _cartClient = cartClient;
    }

    public async Task<OrderDto?> CheckoutAsync(string userId, string bearerToken)
    {
        var cart = await _cartClient.GetCartAsync(bearerToken);
        if (cart is null || cart.Items.Count == 0)
            return null;

        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = "Placed",
            TotalAmount = cart.Total,
            CreatedAt = DateTime.UtcNow,
            Items = cart.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };

        await _repository.AddAsync(order);
        await _cartClient.ClearCartAsync(bearerToken);

        return Map(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(string userId)
    {
        var orders = await _repository.GetByUserIdAsync(userId);
        return orders.Select(Map).ToList();
    }

    public async Task<OrderDto?> GetOrderAsync(string userId, Guid orderId)
    {
        var order = await _repository.GetByIdAsync(orderId);
        return order is null || order.UserId != userId ? null : Map(order);
    }

    private static OrderDto Map(CustomerOrder order) => new(
        order.Id,
        order.Status,
        order.TotalAmount,
        order.CreatedAt,
        order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList());
}
