namespace Mcp.Server.Contracts;

public record OrderLineSummary(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderSummary(string Id, string Status, decimal TotalAmount, DateTime CreatedAt, IReadOnlyList<OrderLineSummary> Items);
