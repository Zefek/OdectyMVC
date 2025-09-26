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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> GaugeByImage(int id, IFormFile file, CancellationToken cancellationToken)
    {
        await gaugeService.SaveFileForGauge(id, file, cancellationToken);
        return Ok();
    }
}
