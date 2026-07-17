using System.Text.Json;

namespace Assistant.Api.Mcp;

public record McpToolInfo(string Name, string Description, JsonElement JsonSchema, bool RequiresBearerToken);
