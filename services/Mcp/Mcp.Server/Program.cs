using Mcp.Server;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var enableConsoleTracing = builder.Configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter");
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("mcp-server"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (enableConsoleTracing)
            tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddConsoleExporter());

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
