using System.Text.Json.Nodes;

namespace Assistant.Api.Anthropic;

public interface IAnthropicClient
{
    Task<JsonObject> SendMessageAsync(JsonArray messages, JsonArray tools, string systemPrompt, CancellationToken ct = default);
}
