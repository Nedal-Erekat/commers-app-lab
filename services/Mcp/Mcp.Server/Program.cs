using Mcp.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<ICommerceClient, GatewayCommerceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Gateway:BaseUrl"]!);
});

builder.Services.AddScoped<CommerceService>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapMcp();

app.Run();
