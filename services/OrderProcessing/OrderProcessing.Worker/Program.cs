using OrderProcessing.Worker;
using OrderProcessing.Worker.Inventory;
using OrderProcessing.Worker.Messaging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

var enableConsoleTracing = builder.Configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter");
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("order-processing-worker"))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();
        if (enableConsoleTracing)
            tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddConsoleExporter());

builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("ServiceBus"));

builder.Services.AddHttpClient<IInventoryClient, CatalogInventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatalogApi:BaseUrl"]!);
});

builder.Services.AddSingleton<OrderPlacedHandler>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
