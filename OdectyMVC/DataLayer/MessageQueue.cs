using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdectyMVC.Business;
using OdectyMVC.Contracts;
using OdectyMVC.Dto;
using OdectyMVC.Options;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

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
            if(model != null)
            {
                await model.BasicPublishAsync(options.Value.ExchangeName, routingKey, true, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))), cancellationToken);
            }
        }
    }
}
