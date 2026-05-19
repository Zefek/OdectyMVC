using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OdectyMVC.HealthChecks;

public class GaugeFileHealthCheck : IHealthCheck
{
    private const string FileName = "GaugeList.json";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(FileName)
            ? HealthCheckResult.Healthy($"{FileName} is present")
            : HealthCheckResult.Unhealthy($"{FileName} is missing"));
}
