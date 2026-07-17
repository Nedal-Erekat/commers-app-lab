using System.Text.Json.Nodes;
using Assistant.Api.Conversations;
using Assistant.Api.Anthropic;
using Assistant.Api.Mcp;

namespace Assistant.Api;

public class AssistantService
{
    private const int MaxIterations = 5;

    private const string SystemPrompt =
        "You are a shopping assistant for Commerce App Lab, an online store. Use the available " +
        "tools to search products, recommend products, check order status, and add items to the " +
        "customer's cart. Be concise and helpful. Only use a tool when it's actually needed to " +
        "answer the customer.";

    private readonly IAnthropicClient _anthropic;
    private readonly IMcpToolClient _mcp;
    private readonly IConversationStore _conversations;

    public AssistantService(IAnthropicClient anthropic, IMcpToolClient mcp, IConversationStore conversations)
    {
        _anthropic = anthropic;
        _mcp = mcp;
        _conversations = conversations;
    }

    public async Task<ChatResult> SendMessageAsync(string? conversationId, string userMessage, string bearerToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("message is required.", nameof(userMessage));

        conversationId ??= Guid.NewGuid().ToString();
        var history = _conversations.GetOrCreate(conversationId);
        history.Add(new JsonObject { ["role"] = "user", ["content"] = userMessage });

        var mcpTools = await _mcp.ListToolsAsync(ct);
        var anthropicTools = BuildAnthropicTools(mcpTools);
        var toolsUsed = new List<string>();

        for (var i = 0; i < MaxIterations; i++)
        {
            var response = await _anthropic.SendMessageAsync(history, anthropicTools, SystemPrompt, ct);
            var content = response["content"]!.AsArray();
            history.Add(new JsonObject { ["role"] = "assistant", ["content"] = content.DeepClone() });

            var toolUseBlocks = content.Where(IsToolUse).ToList();
            if (toolUseBlocks.Count == 0)
                return new ChatResult(conversationId, ExtractText(content), toolsUsed);

            var toolResults = new JsonArray();
            foreach (var block in toolUseBlocks)
            {
                var toolName = block!["name"]!.GetValue<string>();
                var toolUseId = block["id"]!.GetValue<string>();
                var input = block["input"] as JsonObject ?? new JsonObject();

                toolsUsed.Add(toolName);
                var resultText = await CallToolAsync(toolName, input, mcpTools, bearerToken, ct);

                toolResults.Add(new JsonObject
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = toolUseId,
                    ["content"] = resultText
                });
            }

            history.Add(new JsonObject { ["role"] = "user", ["content"] = toolResults });
        }

        return new ChatResult(conversationId, "I wasn't able to finish that — could you try rephrasing?", toolsUsed);
    }

    private async Task<string> CallToolAsync(
        string toolName, JsonObject input, IReadOnlyList<McpToolInfo> mcpTools, string bearerToken, CancellationToken ct)
    {
        var toolInfo = mcpTools.FirstOrDefault(t => t.Name == toolName);
        var arguments = input.ToDictionary(kv => kv.Key, kv => JsonValueToObject(kv.Value));

        if (toolInfo?.RequiresBearerToken == true)
            arguments["bearerToken"] = bearerToken;

        try
        {
            return await _mcp.CallToolAsync(toolName, arguments, ct);
        }
        catch (Exception ex)
        {
            return $"Error calling {toolName}: {ex.Message}";
        }
    }

    private static bool IsToolUse(JsonNode? block) => block?["type"]?.GetValue<string>() == "tool_use";

    private static object? JsonValueToObject(JsonNode? node) => node switch
    {
        null => null,
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        JsonValue v when v.TryGetValue<int>(out var i) => i,
        JsonValue v when v.TryGetValue<double>(out var d) => d,
        JsonValue v when v.TryGetValue<bool>(out var b) => b,
        _ => node.ToJsonString()
    };

    private static JsonArray BuildAnthropicTools(IReadOnlyList<McpToolInfo> tools)
    {
        var array = new JsonArray();
        foreach (var tool in tools)
        {
            var schema = JsonNode.Parse(tool.JsonSchema.GetRawText())!.AsObject();
            if (tool.RequiresBearerToken)
                StripBearerToken(schema);

            array.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["input_schema"] = schema
            });
        }
        return array;
    }

    private static void StripBearerToken(JsonObject schema)
    {
        if (schema["properties"] is JsonObject properties)
            properties.Remove("bearerToken");

        if (schema["required"] is JsonArray required)
        {
            for (var i = required.Count - 1; i >= 0; i--)
            {
                if (required[i]?.GetValue<string>() == "bearerToken")
                    required.RemoveAt(i);
            }
        }
    }

    private static string ExtractText(JsonArray content) => string.Join(
        "\n",
        content.Where(b => b?["type"]?.GetValue<string>() == "text").Select(b => b!["text"]!.GetValue<string>()));
}
