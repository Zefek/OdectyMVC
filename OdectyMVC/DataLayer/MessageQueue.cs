using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OdectyMVC.Contracts;
using OdectyMVC.Options;
using RabbitMQ.Client;
using System.Text;

namespace OdectyMVC.DataLayer
{
    public class MessageQueue : IMessageQueue
    {
        private readonly IChannel model;
        private readonly IOptions<RabbitMQSettings> options;

        public MessageQueue(RabbitMQProvider rabbitMQProvider, IOptions<RabbitMQSettings> options)
        {
            this.options = options;
            model = rabbitMQProvider.CreateModel().Result;
        }

        public async Task Publish(string routingKey, object message, CancellationToken cancellationToken)
        {
            if (model != null)
            {
                await model.BasicPublishAsync(options.Value.ExchangeName, routingKey, true, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))), cancellationToken);
            }
        }
    }
}
