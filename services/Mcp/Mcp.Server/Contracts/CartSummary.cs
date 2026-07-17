namespace Mcp.Server.Contracts;

public record CartLineSummary(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CartSummary(IReadOnlyList<CartLineSummary> Items, decimal Total);
