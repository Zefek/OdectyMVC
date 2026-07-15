using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdectyMVC.Options;
using RabbitMQ.Client;
using System.Security.Authentication;

namespace OdectyMVC.DataLayer;

public class RabbitMQProvider : IAsyncDisposable
{
    private IConnection? connection;
    private readonly IOptions<RabbitMQSettings> options;
    private readonly ILogger<RabbitMQProvider> logger;
    private bool first = true;
    private TimeSpan connectionTimeout = TimeSpan.Zero;
    private readonly Random random = new Random();
    private DateTime? lastAttemptTime = null;
    private TimeSpan? connectionDelay = null;
    private readonly ConnectionFactory factory;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim semaphoreConnect = new SemaphoreSlim(1, 1);

    public bool IsConnected => connection?.IsOpen == true;

    public RabbitMQProvider(IOptions<RabbitMQSettings> options, ILogger<RabbitMQProvider> logger)
    {
        this.options = options;
        this.logger = logger;
        factory = new ConnectionFactory
        {
            HostName = options.Value.HostName,
            UserName = options.Value.UserName,
            Password = options.Value.Password,
            VirtualHost = options.Value.VirtualHost
        };
        if (options.Value.UseTls)
        {
            factory.Port = options.Value.Port ?? 5671;
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = options.Value.TlsServerName ?? options.Value.HostName,
                Version = SslProtocols.Tls12 | SslProtocols.Tls13
            };
        }
        else if (options.Value.Port.HasValue)
        {
            factory.Port = options.Value.Port.Value;
        }
    }

    public async Task<bool> EnsureConnectedAsync()
    {
        if (connection?.IsOpen != true)
        {
            await Connect();
        }
        return IsConnected;
    }

    private async Task Connect()
    {
        if (lastAttemptTime.HasValue && connectionDelay.HasValue && (DateTime.Now - lastAttemptTime.Value) < connectionDelay || connection?.IsOpen == true)
        {
            return;
        }
        await semaphoreConnect.WaitAsync();
        try
        {
            if (connection?.IsOpen != true)
            {
                if (connection != null)
                {
                    try { await connection.CloseAsync(); } catch { }
                    await connection.DisposeAsync();
                    connection = null;
                }
                connection = await factory.CreateConnectionAsync();
                logger.LogInformation("Successfully connected to RabbitMQ at {HostName}", factory.HostName);
                lastAttemptTime = null;
                connectionDelay = null;
                first = true;
            }
        }
        catch (Exception ex)
        {
            lastAttemptTime = DateTime.Now;
            connectionDelay = connectionTimeout = TimeSpan.FromMilliseconds(Math.Min(connectionTimeout.TotalMilliseconds * 2 + random.Next(0, 5000), 300000));
            logger.LogWarning(ex, "Failed to connect to RabbitMQ at {HostName}, retrying in {Delay}ms", factory.HostName, connectionDelay.Value.TotalMilliseconds);
        }
        finally
        {
            semaphoreConnect.Release();
        }
    }

    public async Task<IChannel?> CreateModel()
    {
        if (connection?.IsOpen != true)
        {
            await Connect();
        }
        if (connection?.IsOpen != true)
        {
            return null;
        }
        try
        {
            if (first)
            {
                await semaphore.WaitAsync();
                try
                {
                    if (first)
                    {
                        using var model = await connection.CreateChannelAsync();
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
                finally
                {
                    semaphore.Release();
                }
            }
            return await connection.CreateChannelAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create RabbitMQ channel, will retry.");
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection != null)
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }
        connection = null;
    }
}
