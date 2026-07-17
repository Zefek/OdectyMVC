using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdectyMVC.Contracts;
using OdectyMVC.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OdectyMVC.DataLayer
{
    public class MessageQueue : IMessageQueue
    {
        private readonly RabbitMQProvider rabbitMQProvider;
        private readonly IOptions<RabbitMQSettings> options;
        private readonly ILogger<MessageQueue> logger;
        private IChannel? model;

        public MessageQueue(RabbitMQProvider rabbitMQProvider, IOptions<RabbitMQSettings> options, ILogger<MessageQueue> logger)
        {
            this.rabbitMQProvider = rabbitMQProvider;
            this.options = options;
            this.logger = logger;
        }

        public async Task Publish(string routingKey, object message, CancellationToken cancellationToken)
        {
            if (model == null || model.IsClosed)
            {
                model = await rabbitMQProvider.CreateModel();
            }
            if (model == null)
            {
                logger.LogWarning("Channel could not be created, message not published to {RoutingKey}.", routingKey);
                return;
            }
            await model.BasicPublishAsync(options.Value.ExchangeName, routingKey, true, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))), cancellationToken);
        }
    }
}
