using OdectyMVC.Business;
using OdectyMVC.Contracts;

namespace OdectyMVC.DataLayer
{
    internal class GaugeRepository : IGaugeRepository
    {
        private GaugeDbContext gaugeContext;

        public GaugeRepository(GaugeDbContext gaugeContext)
        {
            this.gaugeContext = gaugeContext;
        }

        public Task<Gauge> GetGauge(int id, CancellationToken cancellationToken)
        {
            return Task.FromResult(gaugeContext.Gauges.FirstOrDefault(k => k.Id == id));
        }
    }
}