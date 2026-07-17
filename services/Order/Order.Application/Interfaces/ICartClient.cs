namespace Order.Application.Interfaces;

public record CartSnapshotItem(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CartSnapshot(IReadOnlyList<CartSnapshotItem> Items, decimal Total);

public interface ICartClient
{
    Task<CartSnapshot?> GetCartAsync(string bearerToken);
    Task ClearCartAsync(string bearerToken);
}
