#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// The ASP.NET types are aliased because DotnetEventBus.Monitoring already declares HealthCheckResult and HealthStatus.
using AspNetHealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using AspNetHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace DotnetEventBus.Monitoring;

/// <summary>
/// ASP.NET Core health check for a RabbitMQ event bus, based on its connection state.
/// Reports Healthy when the connection is open, Degraded while it is reconnecting, and Unhealthy otherwise.
/// Why: lets load balancers and orchestrators see a broker outage without waiting for a publish to fail.
/// </summary>
public sealed class RabbitMqBusHealthCheck : IHealthCheck
{
    private const string ConnectionStateKey = "connectionState";

    private readonly IRabbitMqConnectionMonitor _monitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqBusHealthCheck"/> class.
    /// </summary>
    /// <param name="monitor">The source of the current connection state.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="monitor"/> is null.</exception>
    public RabbitMqBusHealthCheck(IRabbitMqConnectionMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        _monitor = monitor;
    }

    /// <inheritdoc />
    public Task<AspNetHealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var state = _monitor.State;
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            [ConnectionStateKey] = state.ToString()
        };

        var result = state switch
        {
            RabbitMqConnectionState.Open => AspNetHealthCheckResult.Healthy("RabbitMQ connection is open.", data),
            RabbitMqConnectionState.Reconnecting => new AspNetHealthCheckResult(
                AspNetHealthStatus.Degraded, "RabbitMQ connection is reconnecting.", data: data),
            _ => new AspNetHealthCheckResult(
                AspNetHealthStatus.Unhealthy, $"RabbitMQ connection is {state}.", data: data),
        };

        return Task.FromResult(result);
    }
}
