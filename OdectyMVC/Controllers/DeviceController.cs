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
        try
        {
            var r = await gateway.RequestOpen(identity, cancellationToken);
            return Accepted(new GarageCommandResponse(r));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Garage command failed to reach OdectyStat for {Identity}", identity);
            return StatusCode(StatusCodes.Status502BadGateway, "Garage service unavailable");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Garage command to OdectyStat timed out for {Identity}", identity);
            return StatusCode(StatusCodes.Status504GatewayTimeout, "Garage service timed out");
        }
    }
}

public record GarageCommandResponse(uint R);
