using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Application;

namespace OdectyMVC.Controllers;
#if !DEBUG
[Authorize(AuthenticationSchemes = "basic")]
#endif
[ApiController]
[Route("api/[controller]")]
public class FirmwareController : Controller
{
    private readonly IFirmwareService firmwareService;

    public FirmwareController(IFirmwareService firmwareService)
    {
        this.firmwareService = firmwareService;
    }

    [HttpGet("{deviceName}")]
    public async Task<IActionResult> GetUpdate(string deviceName, [FromQuery] int version, CancellationToken cancellationToken)
    {
        return await firmwareService.GetUpdate(deviceName, version, cancellationToken);
    }
}
