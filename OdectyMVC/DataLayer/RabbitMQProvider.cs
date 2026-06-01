using Microsoft.Extensions.Options;
using OdectyMVC.Options;
using RabbitMQ.Client;

namespace OdectyMVC.DataLayer;

public class RabbitMQProvider : IDisposable
{
    private readonly IConnection? connection;
    private readonly IOptions<RabbitMQSettings> options;
    private readonly bool connected = false;
    private bool first = true;

    public bool IsConnected => connected && connection?.IsOpen == true;

    public RabbitMQProvider(IOptions<RabbitMQSettings> options)
    {
        this.options = options;
        try
        {
            var factory = new ConnectionFactory();
            factory.HostName = this.options.Value.HostName;
            factory.UserName = this.options.Value.UserName;
            factory.Password = this.options.Value.Password;
            factory.VirtualHost = this.options.Value.VirtualHost;
            connection = factory.CreateConnectionAsync().Result;
            connected = true;
        }
        catch
        {

        }
    }

    public async Task<IChannel?> CreateModel()
    {
        if (connected)
        {
            if (first)
            {
                using var model = await connection!.CreateChannelAsync();
                await model.ExchangeDeclareAsync(options.Value.ExchangeName, ExchangeType.Direct, true, false, null);
                foreach (var exchange in options.Value.QueueMappings.Select(q => q.ExchangeName).Distinct())
                {
                    if (exchange == null || exchange.StartsWith("amq."))
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
                    if (map.ExchangeName == null || map.RoutingKey == null)
                    {
                        continue;
                    }
                    await model.QueueBindAsync(map.QueueName, map.ExchangeName, map.RoutingKey);
                }
                first = false;
            }
        }
        if (connection != null)
        {
            return await connection.CreateChannelAsync();
        }
        return null;
    }

    public void Dispose()
    {
        connection?.Dispose();
    }
}
