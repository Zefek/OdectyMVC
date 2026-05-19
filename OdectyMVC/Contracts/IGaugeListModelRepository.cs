using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Models;

namespace OdectyMVC.Contracts
{
    public interface IGaugeListModelRepository
    {
        Task<IEnumerable<GaugeListModel>> GetGaugeList(CancellationToken cancellationToken);
        Task<GaugeListModel?> GetById(int id, CancellationToken cancellationToken);
        Task<IActionResult> GetLastPhoto(int id, CancellationToken cancellationToken);
    }
}
