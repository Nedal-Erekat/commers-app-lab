namespace OrderProcessing.Worker.Contracts;

public record OrderPlacedItem(int ProductId, string ProductName, int Quantity);

public record OrderPlacedEvent(Guid OrderId, string UserId, IReadOnlyList<OrderPlacedItem> Items, DateTime PlacedAt);
