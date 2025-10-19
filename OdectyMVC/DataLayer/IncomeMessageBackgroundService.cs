
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OdectyMVC.Application;
using OdectyMVC.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OdectyMVC.DataLayer;

public class IncomeMessageBackgroundService : BackgroundService, IDisposable
{
    private readonly IOptions<RabbitMQSettings> options;
    private readonly IServiceProvider serviceProvider;
    private readonly RabbitMQProvider rabbitMQProvider;
    private IChannel channel;

    public IncomeMessageBackgroundService(IOptions<RabbitMQSettings> options, IServiceProvider serviceProvider, RabbitMQProvider rabbitMQProvider)
    {
        this.options = options;
        this.serviceProvider = serviceProvider;
        this.rabbitMQProvider = rabbitMQProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        channel = await rabbitMQProvider.CreateModel();
        if (channel != null)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                var gaugeState = JsonConvert.DeserializeObject<dynamic>(message);
                using var scope = serviceProvider.CreateScope();
                var gaugeService = scope.ServiceProvider.GetRequiredService<IGaugeService>();
                await gaugeService.UpdateGaugeState((int)gaugeState.gaugeId, (decimal)gaugeState.value, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            };
            await channel.BasicConsumeAsync(queue: QueuesToConsume.OdectyMVC,
                             autoAck: false,
                             consumer: consumer, stoppingToken);
        }
    }

    public override void Dispose()
    {
        channel?.Dispose();
        base.Dispose();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        channel?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
