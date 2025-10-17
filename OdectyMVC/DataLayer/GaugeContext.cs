using OdectyMVC.Contracts;

namespace OdectyMVC.DataLayer
{
    public class GaugeContext : IGaugeContext
    {
        private readonly GaugeDbContext context;

        public GaugeContext(IGaugeRepository gaugeRepository,
            IGaugeListModelRepository gaugeListModelRepository,
            IMessageQueue messageQueue,
            GaugeDbContext context)
        {
            GaugeRepository = gaugeRepository;
            GaugeListModelRepository = gaugeListModelRepository;
            MessageQueue = messageQueue;
            this.context = context;
        }

        public IGaugeRepository GaugeRepository { get; }
        public IGaugeListModelRepository GaugeListModelRepository { get; }
        public IMessageQueue MessageQueue { get; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            context.SaveChanges();
            return Task.CompletedTask;
        }
    }
}
