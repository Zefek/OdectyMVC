using Microsoft.AspNetCore.Mvc;

namespace OdectyMVC.Application
{
    public interface IFirmwareService
    {
        Task<IActionResult> GetUpdate(string deviceName, int currentVersion, CancellationToken cancellationToken);
    }
}
