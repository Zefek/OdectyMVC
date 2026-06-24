using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Contracts;

namespace OdectyMVC.Application
{
    public class FirmwareService : IFirmwareService
    {
        private readonly IFirmwareRepository repository;

        public FirmwareService(IFirmwareRepository repository)
        {
            this.repository = repository;
        }

        public async Task<IActionResult> GetUpdate(string deviceName, int currentVersion, CancellationToken cancellationToken)
        {
            if (!IsValidDeviceName(deviceName))
            {
                return new BadRequestResult();
            }

            var manifest = await repository.GetManifest(deviceName, cancellationToken);
            if (manifest == null)
            {
                return new NotFoundResult();
            }

            if (manifest.Version <= currentVersion)
            {
                return new StatusCodeResult(StatusCodes.Status304NotModified);
            }

            return await repository.GetFirmware(deviceName, cancellationToken);
        }

        private static bool IsValidDeviceName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName) || deviceName == "." || deviceName == "..")
            {
                return false;
            }
            if (deviceName.Contains('/') || deviceName.Contains('\\'))
            {
                return false;
            }
            return deviceName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}
