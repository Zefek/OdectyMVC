using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Application;

namespace OdectyMVC.Controllers;
#if !DEBUG
[Authorize(AuthenticationSchemes = "basic")]
#endif
[ApiController]
[Route("api/[controller]")]
public class GaugeController : Controller
{
    private readonly IGaugeService gaugeService;

    public GaugeController(IGaugeService gaugeService)
    {
        this.gaugeService = gaugeService;
    }

    [HttpPost("{id}")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> GaugeByImage(int id, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        await gaugeService.SaveFileForGauge(id, memoryStream, cancellationToken);
        return Ok();
    }
}
