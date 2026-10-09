using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OdectyMVC.Application;
using System.Globalization;

namespace OdectyMVC.Controllers;
#if !DEBUG
[Authorize(AuthenticationSchemes = "basic")]
#endif
[ApiController]
[Route("api/[controller]")]
public class GaugeController : Controller
{
    private readonly IGaugeService gaugeService;
    private readonly IFirmwareService firmwareService;
    private readonly ILogger<GaugeController> logger;

    public GaugeController(IGaugeService gaugeService, IFirmwareService firmwareService, ILogger<GaugeController> logger)
    {
        this.gaugeService = gaugeService;
        this.firmwareService = firmwareService;
        this.logger = logger;
    }

    [HttpPost("{id}")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> GaugeByImage(int id, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        await gaugeService.SaveFileForGauge(id, memoryStream, ReadCorrelationId(), ReadTransfer(), cancellationToken);
        return Ok();
    }

    private ulong? ReadCorrelationId()
    {
        if (Request.Headers.TryGetValue("X-Correlation-Id", out var value)
            && ulong.TryParse(value.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var correlationId))
        {
            return correlationId;
        }
        return null;
    }

    private byte[]? ReadTransfer()
    {
        // X-Transfer chybí u prvního snímku po bootu; rozbitý hex nesmí shodit upload snímku.
        if (Request.Headers.TryGetValue("X-Transfer", out var value))
        {
            try
            {
                return Convert.FromHexString(value.ToString());
            }
            catch (FormatException)
            {
            }
        }
        return null;
    }

    [HttpGet("{id}/lastphoto")]
    public Task<IActionResult> GetLastPhoto(int id, CancellationToken cancellationToken)
    {
        return gaugeService.GetLastPhoto(id, cancellationToken);
    }

    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    [HttpPost("{id:int}/diag")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> Diagnostics(int id, [FromHeader(Name = "X-Device-Name")] string? deviceName, [FromHeader(Name = "x-ESP32-version")] string? version, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        await gaugeService.SaveDiagnostics(id, memoryStream.ToArray(), cancellationToken);
        var safeDeviceName = SanitizeForLog(deviceName);

        if (!string.IsNullOrEmpty(deviceName) && int.TryParse(version, out var currentVersion))
        {
            try
            {
                if (await firmwareService.HasNewerVersion(deviceName, currentVersion, cancellationToken))
                {
                    Response.Headers.Append("X-OTA-Available", "true");
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "OTA check failed to reach OdectyStat for device {DeviceName}", safeDeviceName);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "OTA check to OdectyStat timed out for device {DeviceName}", safeDeviceName);
            }
        }

        return Ok();
    }

    [HttpPost("{id:int}/config")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> Config(int id, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        await gaugeService.SaveConfig(id, memoryStream.ToArray(), cancellationToken);
        return Ok();
    }
}
