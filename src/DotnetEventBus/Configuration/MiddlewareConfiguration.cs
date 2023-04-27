#nullable enable

using System;
using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Provides extension methods for configuring event bus middleware.
/// </summary>
public static class MiddlewareConfiguration
{
    /// <summary>
    /// Adds a middleware type to the event bus pipeline.
    /// The middleware will be constructed via dependency injection.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of the middleware to add.</typeparam>
    /// <param name="options">The event bus options to configure.</param>
    /// <returns>The modified event bus options.</returns>
    /// <exception cref="ArgumentNullException">Thrown if options is null.</exception>
    /// <exception cref="ArgumentException">Thrown if TMiddleware does not implement IEventBusMiddleware.</exception>
    public static EventBusOptions UseMiddleware<TMiddleware>(this EventBusOptions options)
        where TMiddleware : IEventBusMiddleware
    {
        ArgumentNullException.ThrowIfNull(options);

        var middlewareType = typeof(TMiddleware);
        if (!typeof(IEventBusMiddleware).IsAssignableFrom(middlewareType))
        {
            throw new ArgumentException(
                $"Type {middlewareType.FullName} must implement {typeof(IEventBusMiddleware).FullName}",
                nameof(TMiddleware));
        }

        if (!options.MiddlewareTypes.Contains(middlewareType))
        {
            options.MiddlewareTypes.Add(middlewareType);
        }

        return options;
    }

    /// <summary>
    /// Adds a middleware type to the event bus pipeline if a predicate is true.
    /// The middleware will be constructed via dependency injection.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of the middleware to add.</typeparam>
    /// <param name="options">The event bus options to configure.</param>
    /// <param name="predicate">A function that returns true if the middleware should be added.</param>
    /// <returns>The modified event bus options.</returns>
    public static EventBusOptions UseMiddlewareIf<TMiddleware>(this EventBusOptions options, Func<EventBusOptions, bool> predicate)
        where TMiddleware : IEventBusMiddleware
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(predicate);

        if (predicate(options))
        {
            options.UseMiddleware<TMiddleware>();
        }

        return options;
    }

    /// <summary>
    /// Registers a middleware as a transient service in the DI container.
    /// This should be called for each middleware type added using `UseMiddleware`.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of the middleware to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEventBusMiddleware<TMiddleware>(this IServiceCollection services)
        where TMiddleware : class, IEventBusMiddleware
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<TMiddleware>();
        return services;
    }
}
