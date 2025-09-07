using Microsoft.Extensions.Options;
using OdectyMVC.Options;
using RabbitMQ.Client;

namespace OdectyMVC.DataLayer;

public class RabbitMQProvider : IDisposable
{
    private readonly IConnection connection;
    private readonly IOptions<RabbitMQSettings> options;
    private readonly bool connected = false;

    public RabbitMQProvider(IOptions<RabbitMQSettings> options)
    {
        try
        {
            var factory = new ConnectionFactory();
            factory.HostName = options.Value.HostName;
            factory.UserName = options.Value.UserName;
            factory.Password = options.Value.Password;
            factory.VirtualHost = options.Value.VirtualHost;

            connection = factory.CreateConnectionAsync().Result;
            this.options = options;
            connected = true;
        }
        catch
        {
            
        }
    }

    public async Task<IChannel> CreateModel()
    {
        if (connected)
        {
            var model = await connection.CreateChannelAsync();
            await model.ExchangeDeclareAsync(options.Value.ExchangeName, ExchangeType.Direct, true, false, null);
            foreach (var exchange in options.Value.QueueMappings.Select(q => q.ExchangeName).Distinct())
            {
                if (exchange.StartsWith("amq."))
                {
                    continue;
                }
                await model.ExchangeDeclareAsync(exchange, ExchangeType.Direct, true, false, null);
            }
            foreach (var queue in options.Value.QueueMappings.Select(q => q.QueueName).Distinct())
            {
                await model.QueueDeclareAsync(queue, true, false, false, null);
            }
            foreach (var map in options.Value.QueueMappings)
            {
                await model.QueueBindAsync(map.QueueName, map.ExchangeName, map.RoutingKey);
            }
            return model;
        }
        return null;
    }

    public void Dispose()
    {
        connection?.Dispose();
    }
}
