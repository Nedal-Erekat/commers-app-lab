namespace Cart.Domain.Entities;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
