using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Models;

namespace OdectyMVC.Application
{
    public interface IGaugeService
    {
        Task AddNewValue(int gaugeId, decimal value, CancellationToken cancellationToken);
        Task<IEnumerable<GaugeListModel>> GetGaugeList(CancellationToken cancellationToken);
        Task SaveFileForGauge(int gaugeId, MemoryStream memoryStream, CancellationToken cancellationToken);
        Task<IActionResult> GetLastPhoto(int gaugeId, CancellationToken cancellationToken);
    }
}
