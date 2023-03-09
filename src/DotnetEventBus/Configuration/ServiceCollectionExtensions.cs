#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;

namespace DotnetEventBus.Configuration;

/// <summary>
/// Extension methods for configuring the event bus in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the event bus and related services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Action<EventBusOptions>? configureOptions = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var options = new EventBusOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<IEventMessageRepository, InMemoryEventMessageRepository>();
        services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
        services.AddSingleton<IDeadLetterRepository, InMemoryDeadLetterRepository>();
        services.AddSingleton<EventFormatterFactory>(); // Added for Issue #13
        services.AddSingleton<IEventFormatter, Formatters.JsonEventFormatter>(); // Added for Issue #13, default
        services.AddSingleton<IEventBus>(sp => // Modified for Issue #13
            new Services.EventBus(
                options,
                sp.GetService<Microsoft.Extensions.Logging.ILogger<Services.EventBus>>(),
                sp.GetRequiredService<IDeadLetterService>(), // Added for Issue #13
                sp.GetRequiredService<IEventFormatter>(), // Added for Issue #13
                sp.GetRequiredService<IServiceProvider>())); // Added for Issue #16
        services.AddSingleton<IDeadLetterService, DeadLetterService>();
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddSingleton<IHandlerInvoker, HandlerInvoker>();

        return services;
    }

    /// <summary>
    /// Adds the event bus with custom repositories.
    /// </summary>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IEventMessageRepository messageRepository,
        ISubscriptionRepository subscriptionRepository,
        IDeadLetterRepository deadLetterRepository,
        Action<EventBusOptions>? configureOptions = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (messageRepository is null)
            throw new ArgumentNullException(nameof(messageRepository));
        if (subscriptionRepository is null)
            throw new ArgumentNullException(nameof(subscriptionRepository));
        if (deadLetterRepository is null)
            throw new ArgumentNullException(nameof(deadLetterRepository));

        var options = new EventBusOptions();
        configureOptions?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton(messageRepository);
        services.AddSingleton(subscriptionRepository);
        services.AddSingleton(deadLetterRepository);
        services.AddSingleton<IEventBus>(sp =>
            new Services.EventBus(
                messageRepository,
                subscriptionRepository,
                deadLetterRepository,
                sp.GetRequiredService<IDeadLetterService>(),
                sp.GetRequiredService<IEventFormatter>(),
                sp.GetRequiredService<IServiceProvider>(), // Add IServiceProvider here
                options,
                sp.GetService<Microsoft.Extensions.Logging.ILogger<Services.EventBus>>()));
        services.AddSingleton<IDeadLetterService, DeadLetterService>();
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddSingleton<IHandlerInvoker, HandlerInvoker>();

        return services;
    }
}
