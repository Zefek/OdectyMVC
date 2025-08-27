using OdectyMVC.Contracts;

namespace OdectyMVC.Application
{
    public class GaugeService : IGaugeService
    {
        private readonly IGaugeContext context;

        public GaugeService(IGaugeContext context)
        {
            this.context=context;
        }

        public async Task AddNewValue(int gaugeId, decimal value)
        {
            var gauge = await context.GaugeRepository.GetGauge(gaugeId);
            gauge.SetNewValue(value);
            await context.SaveChangesAsync();
            await context.MessageQueue.Publish(gaugeId, value, DateTime.Now);
        }

        public async Task<IEnumerable<Models.GaugeListModel>> GetGaugeList()
        {
            return await context.GaugeListModelRepository.GetGaugeList();
        }
    }
}
