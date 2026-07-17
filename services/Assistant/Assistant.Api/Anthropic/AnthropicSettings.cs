namespace Assistant.Api.Anthropic;

public class AnthropicSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-5";
    public int MaxTokens { get; set; } = 1024;
}
