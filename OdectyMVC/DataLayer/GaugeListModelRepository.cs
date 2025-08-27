using Microsoft.EntityFrameworkCore;
using OdectyMVC.Contracts;
using OdectyMVC.Models;

namespace OdectyMVC.DataLayer
{
    internal class GaugeListModelRepository : IGaugeListModelRepository
    {
        private GaugeDbContext gaugeContext;

        public GaugeListModelRepository(GaugeDbContext gaugeContext)
        {
            this.gaugeContext=gaugeContext;
        }

        public Task<IEnumerable<GaugeListModel>> GetGaugeList()
        {
            return Task.FromResult(gaugeContext.GaugeModels.OrderBy(k => k.Id) as IEnumerable<GaugeListModel>);
        }
    }
}