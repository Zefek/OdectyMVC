using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdectyMVC.Business;
using OdectyMVC.Contracts;
using RabbitMQ.Client;
using System.Text;

namespace OdectyMVC.DataLayer
{
    public class MessageQueue : IMessageQueue
    {
        private readonly IConnection connection;
        private readonly IModel model;
        private readonly IOptions<RabbitMQSettings> options;

        public MessageQueue(IOptions<RabbitMQSettings> options) 
        {
            var factory = new ConnectionFactory();
            factory.HostName = options.Value.HostName;
            factory.UserName = options.Value.UserName;
            factory.Password = options.Value.Password;

            connection = factory.CreateConnection();
            model = connection.CreateModel();
            this.options = options;
        }

        public Task Publish(int gaugeId, decimal value, DateTime datetime)
        {
            var newValue = new
            {
                GaugeId = gaugeId,
                Value = value,
                Datetime = datetime
            };
            model.BasicPublish(options.Value.ExchangeName, gaugeId.ToString(), null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(newValue)));
            return Task.CompletedTask;
        }
    }
}
