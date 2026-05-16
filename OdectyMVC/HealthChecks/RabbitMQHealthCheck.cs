using Microsoft.Extensions.Diagnostics.HealthChecks;
using OdectyMVC.DataLayer;

namespace OdectyMVC.HealthChecks;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly RabbitMQProvider provider;

    public RabbitMQHealthCheck(RabbitMQProvider provider) => this.provider = provider;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(provider.IsConnected
            ? HealthCheckResult.Healthy("RabbitMQ connection is open")
            : HealthCheckResult.Unhealthy("RabbitMQ connection is not available"));
}
