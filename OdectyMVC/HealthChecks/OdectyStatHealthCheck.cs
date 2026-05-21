using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OdectyMVC.HealthChecks;

public class OdectyStatHealthCheck : IHealthCheck
{
    private readonly HttpClient httpClient;

    public OdectyStatHealthCheck(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("health/ready", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"OdectyStat /health/ready returned {(int)response.StatusCode}")
                : HealthCheckResult.Unhealthy($"OdectyStat /health/ready returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("OdectyStat is unreachable", ex);
        }
    }
}
