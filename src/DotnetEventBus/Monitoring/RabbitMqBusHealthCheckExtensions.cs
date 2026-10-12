#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotnetEventBus.Monitoring;

/// <summary>
/// Extension methods that register <see cref="RabbitMqBusHealthCheck"/>.
/// </summary>
public static class RabbitMqBusHealthCheckExtensions
{
    /// <summary>
    /// Registers the RabbitMQ event bus health check with <c>AddHealthChecks()</c>.
    /// Uses the <see cref="IRabbitMqConnectionMonitor"/> registered in the container. If none is registered,
    /// the check reports Unhealthy, because there is no RabbitMQ connection to report on.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The name of the health check. Defaults to <c>eventbus</c>.</param>
    /// <param name="tags">Optional tags used to filter health check endpoints.</param>
    /// <returns>The health checks builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
    public static IHealthChecksBuilder AddEventBusHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "eventbus",
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IRabbitMqConnectionMonitor>(new UnconfiguredRabbitMqConnectionMonitor());

        return builder.AddCheck<RabbitMqBusHealthCheck>(name, tags: tags);
    }

    /// <summary>
    /// Stands in when no RabbitMQ connection monitor is registered. It reports Closed, so the check is Unhealthy.
    /// </summary>
    private sealed class UnconfiguredRabbitMqConnectionMonitor : IRabbitMqConnectionMonitor
    {
        public RabbitMqConnectionState State => RabbitMqConnectionState.Closed;
    }
}
