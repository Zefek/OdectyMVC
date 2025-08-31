
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OdectyMVC.Application;
using OdectyMVC.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OdectyMVC.DataLayer;

public class IncomeMessageBackgroundService : BackgroundService, IDisposable
{
    private readonly IOptions<RabbitMQSettings> options;
    private readonly IServiceProvider serviceProvider;
    private readonly RabbitMQProvider rabbitMQProvider;
    private IModel channel;

    public IncomeMessageBackgroundService(IOptions<RabbitMQSettings> options, IServiceProvider serviceProvider, RabbitMQProvider rabbitMQProvider)
    {
        this.options = options;
        this.serviceProvider = serviceProvider;
        this.rabbitMQProvider = rabbitMQProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        channel = rabbitMQProvider.CreateModel();
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = System.Text.Encoding.UTF8.GetString(body);
            var gaugeState = JsonConvert.DeserializeObject<dynamic>(message);
            using var scope = serviceProvider.CreateScope();
            var gaugeService = scope.ServiceProvider.GetRequiredService<IGaugeService>();
            await gaugeService.UpdateGaugeState(gaugeState.gaugeId, gaugeState.value);
            channel.BasicAck(ea.DeliveryTag, false);
        };
        foreach (var queue in options.Value.QueueMappings.Select(q => q.QueueName).Distinct())
        {
            channel.BasicConsume(queue: queue,
                             autoAck: false,
                             consumer: consumer);
        }
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        channel?.Close();
        base.Dispose();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        channel?.Close();
        return base.StopAsync(cancellationToken);
    }
}
