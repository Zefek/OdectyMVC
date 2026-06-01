namespace OdectyMVC.DataLayer;

public class QueueMapping
{
    public required string QueueName { get; set; }
    public string? RoutingKey { get; set; }
    public string? ExchangeName { get; set; }
}
