using OrderProcessing.Worker;
using OrderProcessing.Worker.Inventory;
using OrderProcessing.Worker.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("ServiceBus"));

builder.Services.AddHttpClient<IInventoryClient, CatalogInventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatalogApi:BaseUrl"]!);
});

builder.Services.AddSingleton<OrderPlacedHandler>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
