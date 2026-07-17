namespace OrderProcessing.Worker.Inventory;

public interface IInventoryClient
{
    Task<bool> DecrementStockAsync(int productId, int quantity);
}
