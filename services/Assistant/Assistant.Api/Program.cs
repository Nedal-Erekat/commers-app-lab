using System.Text;
using Assistant.Api;
using Assistant.Api.Anthropic;
using Assistant.Api.Conversations;
using Assistant.Api.Mcp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.Configure<McpSettings>(builder.Configuration.GetSection("Mcp"));
builder.Services.Configure<AnthropicSettings>(builder.Configuration.GetSection("Anthropic"));

builder.Services.AddSingleton<IMcpToolClient, McpToolClient>();
builder.Services.AddHttpClient<IAnthropicClient, AnthropicClient>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
});
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddScoped<AssistantService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapPost("/api/chat", async (ChatRequest request, AssistantService assistant, ILogger<Program> logger, HttpContext http, CancellationToken ct) =>
{
    var bearerToken = ExtractBearerToken(http.Request);
    try
    {
        var result = await assistant.SendMessageAsync(request.ConversationId, request.Message, bearerToken, ct);
        return Results.Ok(new { conversationId = result.ConversationId, reply = result.Reply, toolsUsed = result.ToolsUsed });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat request failed");
        return Results.Problem("The assistant couldn't process that message.", statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireAuthorization();

app.Run();

static string ExtractBearerToken(HttpRequest request)
{
    var header = request.Headers.Authorization.ToString();
    const string prefix = "Bearer ";
    return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? header[prefix.Length..] : header;
}
