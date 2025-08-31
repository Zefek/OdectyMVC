using OdectyMVC.Models;

namespace OdectyMVC.Application
{
    public interface IGaugeService
    {
        Task AddNewValue(int gaugeId, decimal value);
        Task<IEnumerable<GaugeListModel>> GetGaugeList();
        Task UpdateGaugeState(int gaugeId, decimal value);
    }
}