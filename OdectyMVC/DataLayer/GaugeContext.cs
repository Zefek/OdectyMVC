using OdectyMVC.Contracts;

namespace OdectyMVC.DataLayer
{
    public class GaugeContext : IGaugeContext
    {
        public GaugeContext(
            IGaugeListModelRepository gaugeListModelRepository,
            IMessageQueue messageQueue)
        {
            GaugeListModelRepository = gaugeListModelRepository;
            MessageQueue = messageQueue;
        }

        public IGaugeListModelRepository GaugeListModelRepository { get; }
        public IMessageQueue MessageQueue { get; }
    }
}
