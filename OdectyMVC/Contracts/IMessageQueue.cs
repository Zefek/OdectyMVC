using OdectyMVC.Business;

namespace OdectyMVC.Contracts
{
    public interface IMessageQueue
    {
        Task Publish(string routingKey, object message, CancellationToken cancellationToken);
    }
}
