using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Models;

namespace OdectyMVC.Contracts
{
    public interface IFirmwareRepository
    {
        Task<FirmwareManifest?> GetManifest(string deviceName, CancellationToken cancellationToken);
        Task<IActionResult> GetFirmware(string deviceName, CancellationToken cancellationToken);
    }
}
