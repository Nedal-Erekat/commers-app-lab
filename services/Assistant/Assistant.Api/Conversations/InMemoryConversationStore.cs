using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace Assistant.Api.Conversations;

// Lost on restart — acceptable for a lab; a real deployment would back this
// with Redis or a database keyed by user + conversation.
public class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, JsonArray> _conversations = new();

    public JsonArray GetOrCreate(string conversationId) =>
        _conversations.GetOrAdd(conversationId, _ => new JsonArray());
}
