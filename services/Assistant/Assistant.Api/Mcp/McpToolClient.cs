using System.Linq;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Options;

namespace Assistant.Api.Mcp;

public class McpToolClient : IMcpToolClient, IAsyncDisposable
{
    private readonly Uri _endpoint;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private McpClient? _client;

    public McpToolClient(IOptions<McpSettings> options)
    {
        _endpoint = new Uri(options.Value.BaseUrl);
    }

    public async Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(CancellationToken ct = default)
    {
        var client = await GetClientAsync(ct);
        var tools = await client.ListToolsAsync(cancellationToken: ct);

        return tools.Select(t => new McpToolInfo(
            t.Name,
            t.Description ?? string.Empty,
            t.JsonSchema,
            RequiresBearerToken(t.JsonSchema))).ToList();
    }

    public async Task<string> CallToolAsync(string name, IReadOnlyDictionary<string, object?> arguments, CancellationToken ct = default)
    {
        var client = await GetClientAsync(ct);
        var result = await client.CallToolAsync(name, arguments, cancellationToken: ct);

        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        return text ?? (result.IsError == true ? $"Tool '{name}' failed." : string.Empty);
    }

    private async Task<McpClient> GetClientAsync(CancellationToken ct)
    {
        if (_client is not null) return _client;

        await _lock.WaitAsync(ct);
        try
        {
            _client ??= await McpClient.CreateAsync(
                new HttpClientTransport(new HttpClientTransportOptions { Endpoint = _endpoint }),
                cancellationToken: ct);
            return _client;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static bool RequiresBearerToken(System.Text.Json.JsonElement schema) =>
        schema.TryGetProperty("properties", out var props) && props.TryGetProperty("bearerToken", out _);

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }
}
