using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace OdectyMVC.DataLayer;

public class RabbitMQProvider : IDisposable
{
    private readonly IConnection connection;
    private readonly IOptions<RabbitMQSettings> options;

    public RabbitMQProvider(IOptions<RabbitMQSettings> options)
    {
        var factory = new ConnectionFactory();
        factory.HostName = options.Value.HostName;
        factory.UserName = options.Value.UserName;
        factory.Password = options.Value.Password;
        factory.VirtualHost = options.Value.VirtualHost;

        connection = factory.CreateConnection();
        this.options = options;
    }

    public IModel CreateModel()
    {
        var model = connection.CreateModel();
        model.ExchangeDeclare(options.Value.ExchangeName, ExchangeType.Direct, true, false, null);
        foreach (var exchange in options.Value.QueueMappings.Select(q => q.ExchangeName).Distinct())
        {
            if (exchange.StartsWith("amq."))
            {
                continue;
            }
            model.ExchangeDeclare(exchange, ExchangeType.Direct, true, false, null);
        }
        foreach (var queue in options.Value.QueueMappings.Select(q => q.QueueName).Distinct())
        {
            model.QueueDeclare(queue, true, false, false, null);
        }
        foreach (var map in options.Value.QueueMappings)
        {
            model.QueueBind(map.QueueName, map.ExchangeName, map.RoutingKey);
        }
        return model;
    }

    public void Dispose()
    {
        connection?.Close();
    }
}
