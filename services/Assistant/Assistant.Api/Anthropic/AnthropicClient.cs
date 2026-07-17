using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Assistant.Api.Anthropic;

public class AnthropicClient : IAnthropicClient
{
    private readonly HttpClient _http;
    private readonly AnthropicSettings _settings;

    public AnthropicClient(HttpClient http, IOptions<AnthropicSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task<JsonObject> SendMessageAsync(JsonArray messages, JsonArray tools, string systemPrompt, CancellationToken ct = default)
    {
        var requestBody = new JsonObject
        {
            ["model"] = _settings.Model,
            ["max_tokens"] = _settings.MaxTokens,
            ["system"] = systemPrompt,
            ["messages"] = messages.DeepClone(),
            ["tools"] = tools.DeepClone()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", _settings.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await _http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Anthropic API returned {(int)response.StatusCode}: {responseBody}");

        return JsonNode.Parse(responseBody)!.AsObject();
    }
}
