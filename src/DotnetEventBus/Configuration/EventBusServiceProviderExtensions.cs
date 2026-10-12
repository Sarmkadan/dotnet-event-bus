#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Extension methods for resolving the event bus from a service provider.
/// </summary>
public static class EventBusServiceProviderExtensions
{
    /// <summary>
    /// Resolves <see cref="IEventBus"/>, failing with a message that points at <c>AddEventBus</c>
    /// when no bus was registered.
    /// </summary>
    /// <param name="provider">The service provider to resolve from.</param>
    /// <returns>The registered event bus.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when no event bus is registered in the container.</exception>
    public static IEventBus GetRequiredEventBus(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (provider.GetService<EventBusRegistration>() is null)
        {
            throw new InvalidOperationException(
                "No event bus is registered. Call services.AddEventBus() before resolving IEventBus.");
        }

        return provider.GetRequiredService<IEventBus>();
    }
}
