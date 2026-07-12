using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OdectyMVC.Contracts;

namespace OdectyMVC.Controllers;

[ApiController]
[Route("device")]
public class DeviceController : ControllerBase
{
    private readonly IGarageGateway gateway;
    private readonly ILogger<DeviceController> logger;

    public DeviceController(IGarageGateway gateway, ILogger<DeviceController> logger)
    {
        this.gateway = gateway;
        this.logger = logger;
    }

    [HttpPost("garageCommand")]
#if !DEBUG
    [Authorize(AuthenticationSchemes = "basic", Roles = "GarageOperator")]
#endif
    [EnableRateLimiting("garage")]
    public async Task<IActionResult> GarageCommand(CancellationToken cancellationToken)
    {
        var identity = User.Identity?.Name ?? "unknown";
        logger.LogInformation("Garage open requested by {Identity}", identity);
        var r = await gateway.RequestOpen(identity, cancellationToken);
        return Accepted(new { r });
    }
}
