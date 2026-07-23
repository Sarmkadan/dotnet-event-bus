#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotnetEventBus.Formatters;
using DotnetEventBus.Integration;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using DotnetEventBus.Transport;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Extension methods for configuring the event bus in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the event bus and related services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">Optional configuration action for event bus options.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Action<EventBusOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EventBusOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        RegisterConfiguredMiddleware(services, options);
        services.AddSingleton<IEventMessageRepository, InMemoryEventMessageRepository>();
        services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
        services.AddSingleton<IDeadLetterRepository, InMemoryDeadLetterRepository>();
        services.AddSingleton<EventFormatterFactory>();
        services.AddSingleton<IEventFormatter, Formatters.JsonEventFormatter>();
        services.AddSingleton<IEventBus>(sp =>
            new Services.EventBus(
                options,
                sp.GetService<Microsoft.Extensions.Logging.ILogger<Services.EventBus>>(),
                sp.GetRequiredService<IDeadLetterService>(),
                sp.GetRequiredService<IEventFormatter>(),
                sp.GetRequiredService<IServiceProvider>(),
                sp.GetRequiredService<IEventMessageRepository>(),
                sp.GetRequiredService<ISubscriptionRepository>(),
                sp.GetRequiredService<IDeadLetterRepository>()));
        // Resolves IEventBus lazily (via IServiceProvider) instead of taking it as a
        // direct constructor dependency: IEventBus's own factory above resolves
        // IDeadLetterService, so an eager IEventBus dependency here would re-enter the
        // still-in-progress EventBus singleton resolution and deadlock on first use.
        services.AddSingleton<IDeadLetterService>(sp =>
            new DeadLetterService(
                sp.GetRequiredService<IDeadLetterRepository>(),
                sp.GetRequiredService<IServiceProvider>(),
                sp.GetService<Microsoft.Extensions.Logging.ILogger<DeadLetterService>>()));
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddSingleton<IHandlerInvoker, HandlerInvoker>();
        services.AddSingleton<ITransportRegistry, TransportRegistry>();

        return services;
    }

    /// <summary>
    /// Adds the event bus with custom repositories to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="messageRepository">The event message repository implementation.</param>
    /// <param name="subscriptionRepository">The subscription repository implementation.</param>
    /// <param name="deadLetterRepository">The dead letter repository implementation.</param>
    /// <param name="configureOptions">Optional configuration action for event bus options.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any parameter is null.</exception>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IEventMessageRepository messageRepository,
        ISubscriptionRepository subscriptionRepository,
        IDeadLetterRepository deadLetterRepository,
        Action<EventBusOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageRepository);
        ArgumentNullException.ThrowIfNull(subscriptionRepository);
        ArgumentNullException.ThrowIfNull(deadLetterRepository);

        var options = new EventBusOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        RegisterConfiguredMiddleware(services, options);
        services.AddSingleton(messageRepository);
        services.AddSingleton(subscriptionRepository);
        services.AddSingleton(deadLetterRepository);
        services.AddSingleton<IEventFormatter, Formatters.JsonEventFormatter>();
        // See remarks in the other AddEventBus overload: IDeadLetterService must resolve
        // IEventBus lazily (via IServiceProvider), not as a direct constructor dependency,
        // or the two singletons deadlock resolving each other on first use.
        services.AddSingleton<IDeadLetterService>(sp =>
            new DeadLetterService(
                deadLetterRepository,
                sp.GetRequiredService<IServiceProvider>(),
                sp.GetService<Microsoft.Extensions.Logging.ILogger<DeadLetterService>>()));
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddSingleton<IHandlerInvoker, HandlerInvoker>();
        services.AddSingleton<IEventBus>(sp =>
            new Services.EventBus(
                messageRepository,
                subscriptionRepository,
                deadLetterRepository,
                sp.GetRequiredService<IDeadLetterService>(),
                sp.GetRequiredService<IEventFormatter>(),
                sp.GetRequiredService<IServiceProvider>(),
                options,
                sp.GetService<Microsoft.Extensions.Logging.ILogger<Services.EventBus>>()));

        return services;
    }

    /// <summary>
    /// Makes sure every middleware type configured via <c>options.UseMiddleware&lt;T&gt;()</c> is
    /// actually resolvable from the container. EventBus resolves these types at publish time
    /// with <c>GetRequiredService</c>, so a configured-but-unregistered middleware would throw
    /// on the first publish. TryAdd keeps any registration the caller made explicitly
    /// (e.g. via <see cref="MiddlewareConfiguration.AddEventBusMiddleware{TMiddleware}"/> with a
    /// different lifetime) intact.
    /// </summary>
    private static void RegisterConfiguredMiddleware(IServiceCollection services, EventBusOptions options)
    {
        foreach (var middlewareType in options.MiddlewareTypes)
        {
            services.TryAddTransient(middlewareType);
        }
    }

    /// <summary>
    /// Adds and configures the default in-process transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for the event bus.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInProcessTransport(
        this IServiceCollection services,
        Action<EventBusOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add the event bus first
        services.AddEventBus(configureOptions);

        // Register the in-process transport as the default
        services.AddSingleton<IEventTransport>(sp =>
        {
            var eventBus = sp.GetRequiredService<IEventBus>();
            return new InProcessTransport(eventBus);
        });

        return services;
    }

    /// <summary>
    /// Adds and configures the webhook transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="signingSecret">Optional signing secret for webhook signature verification.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddWebhookTransport(
        this IServiceCollection services,
        string? signingSecret = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register webhook handler
        services.AddSingleton<WebhookHandler>(sp => new WebhookHandler(signingSecret));

        // Register webhook transport
        services.AddSingleton<IEventTransport>(sp =>
        {
            var webhookHandler = sp.GetRequiredService<WebhookHandler>();
            return new WebhookTransport(webhookHandler);
        });

        return services;
    }

    /// <summary>
    /// Configures the transport registry with the specified transports as default.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="transportIds">The transport IDs to set as default (use "in-process-transport" for in-process).</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection ConfigureTransportRegistry(
        this IServiceCollection services,
        params string[] transportIds)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITransportRegistry>(sp =>
        {
            var registry = new TransportRegistry();
            var serviceProvider = sp;

            // Find and register all transports
            foreach (var transport in serviceProvider.GetServices<IEventTransport>() ?? Array.Empty<IEventTransport>())
            {
                registry.RegisterTransport(transport);
            }

            // Set default transport if specified
            if (transportIds?.Length > 0)
            {
                foreach (var transportId in transportIds)
                {
                    var transport = serviceProvider.GetServices<IEventTransport>()
                        .FirstOrDefault(t => t.TransportId == transportId);

                    if (transport != null)
                    {
                        registry.RegisterTransport(transport, isDefault: true);
                        break; // Use first match
                    }
                }
            }

            return registry;
        });

        return services;
    }
}