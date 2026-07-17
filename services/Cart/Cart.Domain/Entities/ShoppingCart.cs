namespace Cart.Domain.Entities;

public class ShoppingCart
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = [];
}
