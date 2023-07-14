#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus.Formatters;
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
                sp.GetRequiredService<IServiceProvider>()));
        services.AddSingleton<IDeadLetterService, DeadLetterService>();
        services.AddSingleton<ISubscriptionManager, SubscriptionManager>();
        services.AddSingleton<IHandlerInvoker, HandlerInvoker>();

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
        services.AddSingleton(messageRepository);
        services.AddSingleton(subscriptionRepository);
        services.AddSingleton(deadLetterRepository);
        services.AddSingleton<IEventFormatter, Formatters.JsonEventFormatter>();
        services.AddSingleton<IDeadLetterService, DeadLetterService>();
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
}