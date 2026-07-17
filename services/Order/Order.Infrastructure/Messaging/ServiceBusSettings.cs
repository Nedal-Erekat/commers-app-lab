namespace Order.Infrastructure.Messaging;

public class ServiceBusSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "order-placed";
}
