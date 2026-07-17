namespace Assistant.Api.Mcp;

public interface IMcpToolClient
{
    Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(CancellationToken ct = default);
    Task<string> CallToolAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken ct = default);
}
