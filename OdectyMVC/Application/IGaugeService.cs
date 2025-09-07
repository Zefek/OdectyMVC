using OdectyMVC.Models;

namespace OdectyMVC.Application
{
    public interface IGaugeService
    {
        Task AddNewValue(int gaugeId, decimal value, CancellationToken cancellationToken);
        Task<IEnumerable<GaugeListModel>> GetGaugeList(CancellationToken cancellationToken);
        Task UpdateGaugeState(int gaugeId, decimal value, CancellationToken cancellationToken);
        Task SaveFileForGauge(int gaugeId, IFormFile file, CancellationToken cancellationToken);
    }
}