namespace Order.Application.Interfaces;

public record OrderPlacedItem(int ProductId, string ProductName, int Quantity);

public record OrderPlacedEvent(Guid OrderId, string UserId, IReadOnlyList<OrderPlacedItem> Items, DateTime PlacedAt);

public interface IEventPublisher
{
    Task PublishOrderPlacedAsync(OrderPlacedEvent orderPlacedEvent);
}
