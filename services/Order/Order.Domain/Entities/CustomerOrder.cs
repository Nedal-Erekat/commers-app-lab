namespace Order.Domain.Entities;

public class CustomerOrder
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = "Placed";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
