using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdectyMVC.Business;
using OdectyMVC.Contracts;
using OdectyMVC.Dto;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

namespace OdectyMVC.DataLayer
{
    public class MessageQueue : IMessageQueue
    {
        private readonly IConnection connection;
        private readonly IModel model;
        private readonly IOptions<RabbitMQSettings> options;

        public MessageQueue(RabbitMQProvider rabbitMQProvider, IOptions<RabbitMQSettings> options) 
        {
            this.options = options;
            model = rabbitMQProvider.CreateModel();
        }

        public Task Publish(int gaugeId, decimal value, DateTime datetime)
        {
            var newValue = new
            {
                GaugeId = gaugeId,
                Value = value,
                Datetime = datetime
            };
            model.BasicPublish(options.Value.ExchangeName, MessageQueueRoutingKeys.GaugeMVC_Gauge_Statechanged, null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(newValue)));
            return Task.CompletedTask;
        }
    }
}
