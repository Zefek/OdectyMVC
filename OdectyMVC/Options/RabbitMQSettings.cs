using OdectyMVC.DataLayer;

namespace OdectyMVC.Options;

public class RabbitMQSettings
{
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public string ExchangeName { get; set; }
    public List<QueueMapping> QueueMappings { get; set; } = new();
}
