namespace Assistant.Api;

public record ChatResult(string ConversationId, string Reply, IReadOnlyList<string> ToolsUsed);
