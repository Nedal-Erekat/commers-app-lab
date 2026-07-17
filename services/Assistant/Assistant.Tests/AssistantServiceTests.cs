using System.Text.Json;
using System.Text.Json.Nodes;
using Assistant.Api;
using Assistant.Api.Anthropic;
using Assistant.Api.Conversations;
using Assistant.Api.Mcp;
using Moq;

namespace Assistant.Tests;

public class AssistantServiceTests
{
    private readonly Mock<IAnthropicClient> _anthropicMock = new();
    private readonly Mock<IMcpToolClient> _mcpMock = new();
    private readonly InMemoryConversationStore _conversations = new();
    private readonly AssistantService _sut;

    public AssistantServiceTests()
    {
        _mcpMock.Setup(m => m.ListToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<McpToolInfo>
            {
                new("search-products", "Search products", ParseSchema("""{"type":"object","properties":{"query":{"type":"string"}},"required":["query"]}"""), false),
                new("get-order-status", "Get order status", ParseSchema("""{"type":"object","properties":{"bearerToken":{"type":"string"},"orderId":{"type":"string"}},"required":["bearerToken","orderId"]}"""), true)
            });

        _sut = new AssistantService(_anthropicMock.Object, _mcpMock.Object, _conversations);
    }

    private static JsonElement ParseSchema(string json) => JsonDocument.Parse(json).RootElement;

    private static JsonObject TextResponse(string text) => new()
    {
        ["content"] = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = text } }
    };

    private static JsonObject ToolUseResponse(string toolUseId, string toolName, JsonObject input) => new()
    {
        ["content"] = new JsonArray
        {
            new JsonObject { ["type"] = "tool_use", ["id"] = toolUseId, ["name"] = toolName, ["input"] = input }
        }
    };

    [Fact]
    public async Task SendMessageAsync_ThrowsArgumentException_WhenMessageBlank()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SendMessageAsync(null, "  ", "token"));
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsText_WhenNoToolNeeded()
    {
        _anthropicMock.Setup(a => a.SendMessageAsync(It.IsAny<JsonArray>(), It.IsAny<JsonArray>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TextResponse("Hi, how can I help you shop today?"));

        var result = await _sut.SendMessageAsync(null, "hello", "token");

        Assert.Equal("Hi, how can I help you shop today?", result.Reply);
        Assert.Empty(result.ToolsUsed);
        _mcpMock.Verify(m => m.CallToolAsync(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_ExecutesToolAndReturnsFollowUpText()
    {
        var callCount = 0;
        _anthropicMock.Setup(a => a.SendMessageAsync(It.IsAny<JsonArray>(), It.IsAny<JsonArray>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? ToolUseResponse("toolu_1", "search-products", new JsonObject { ["query"] = "widget" })
                    : TextResponse("I found a Widget for $9.99.");
            });

        _mcpMock.Setup(m => m.CallToolAsync("search-products", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"id":1,"name":"Widget","price":9.99}]""");

        var result = await _sut.SendMessageAsync(null, "find me a widget", "token");

        Assert.Equal("I found a Widget for $9.99.", result.Reply);
        Assert.Equal(["search-products"], result.ToolsUsed);
        _mcpMock.Verify(m => m.CallToolAsync(
            "search-products",
            It.Is<IReadOnlyDictionary<string, object?>>(args => (string)args["query"]! == "widget"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_InjectsBearerToken_ForAuthRequiredTools()
    {
        var callCount = 0;
        _anthropicMock.Setup(a => a.SendMessageAsync(It.IsAny<JsonArray>(), It.IsAny<JsonArray>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? ToolUseResponse("toolu_1", "get-order-status", new JsonObject { ["orderId"] = "order-1" })
                    : TextResponse("Your order is Placed.");
            });

        _mcpMock.Setup(m => m.CallToolAsync("get-order-status", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"status":"Placed"}""");

        await _sut.SendMessageAsync(null, "where's my order?", "user-jwt-token");

        _mcpMock.Verify(m => m.CallToolAsync(
            "get-order-status",
            It.Is<IReadOnlyDictionary<string, object?>>(args =>
                (string)args["orderId"]! == "order-1" && (string)args["bearerToken"]! == "user-jwt-token"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_DoesNotExposeBearerTokenInToolSchemaSentToAnthropic()
    {
        JsonArray? capturedTools = null;
        _anthropicMock.Setup(a => a.SendMessageAsync(It.IsAny<JsonArray>(), It.IsAny<JsonArray>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<JsonArray, JsonArray, string, CancellationToken>((_, tools, _, _) => capturedTools = tools)
            .ReturnsAsync(TextResponse("ok"));

        await _sut.SendMessageAsync(null, "hi", "token");

        var orderStatusTool = capturedTools!.Single(t => t!["name"]!.GetValue<string>() == "get-order-status");
        var properties = orderStatusTool!["input_schema"]!["properties"]!.AsObject();
        Assert.False(properties.ContainsKey("bearerToken"));
    }

    [Fact]
    public async Task SendMessageAsync_StopsAfterMaxIterations_WhenModelNeverFinishes()
    {
        _anthropicMock.Setup(a => a.SendMessageAsync(It.IsAny<JsonArray>(), It.IsAny<JsonArray>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ToolUseResponse("toolu_x", "search-products", new JsonObject { ["query"] = "x" }));

        _mcpMock.Setup(m => m.CallToolAsync("search-products", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        var result = await _sut.SendMessageAsync(null, "loop forever", "token");

        Assert.Contains("try rephrasing", result.Reply);
        _anthropicMock.Verify(a => a.SendMessageAsync(It.IsAny<JsonArray>(), It.IsAny<JsonArray>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }
}
