using System.Text.Json.Nodes;

namespace Assistant.Api.Conversations;

public interface IConversationStore
{
    JsonArray GetOrCreate(string conversationId);
}
