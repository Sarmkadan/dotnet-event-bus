#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetEventBus.Configuration;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetEventBus;

/// <summary>
/// Fluent builder for configuring and creating the event bus.
/// </summary>
public sealed class EventBusBuilder
{
    private EventBusOptions _options = new();
    private IEventMessageRepository? _messageRepository;
    private ISubscriptionRepository? _subscriptionRepository;
    private IDeadLetterRepository? _deadLetterRepository;
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the EventBusBuilder class.
    /// </summary>
    /// <param name="services">The service collection the event bus will be registered in.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public EventBusBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    /// <summary>
    /// Configures the event bus options.
    /// </summary>
    /// <param name="configureOptions">A delegate that mutates the <see cref="EventBusOptions"/> instance in place.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureOptions"/> is <see langword="null"/>.</exception>
    public EventBusBuilder WithOptions(Action<EventBusOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        configureOptions(_options);
        return this;
    }

    /// <summary>
    /// Sets a custom event message repository.
    /// </summary>
    /// <param name="repository">The repository used to store event messages.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <see langword="null"/>.</exception>
    public EventBusBuilder WithMessageRepository(IEventMessageRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _messageRepository = repository;
        return this;
    }

    /// <summary>
    /// Sets a custom subscription repository.
    /// </summary>
    /// <param name="repository">The repository used to store event subscriptions.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <see langword="null"/>.</exception>
    public EventBusBuilder WithSubscriptionRepository(ISubscriptionRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _subscriptionRepository = repository;
        return this;
    }

    /// <summary>
    /// Sets a custom dead letter repository.
    /// </summary>
    /// <param name="repository">The repository used to store dead-lettered events.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is <see langword="null"/>.</exception>
    public EventBusBuilder WithDeadLetterRepository(IDeadLetterRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _deadLetterRepository = repository;
        return this;
    }

    /// <summary>
    /// Sets the maximum retry attempts.
    /// </summary>
    /// <param name="maxAttempts">The maximum number of retry attempts. Must be zero or greater.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxAttempts"/> is negative.</exception>
    public EventBusBuilder WithMaxRetries(int maxAttempts)
    {
        if (maxAttempts < 0)
            throw new ArgumentException("Max retry attempts cannot be negative", nameof(maxAttempts));

        _options.MaxRetryAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the default handler timeout.
    /// </summary>
    /// <param name="timeout">The timeout applied to handlers. Must be greater than zero.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="timeout"/> is zero or negative.</exception>
    public EventBusBuilder WithHandlerTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero", nameof(timeout));

        _options.DefaultHandlerTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables or disables parallel handler execution.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to run handlers in parallel; <see langword="false"/> to run them sequentially.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public EventBusBuilder WithParallelHandling(bool enabled)
    {
        _options.AllowParallelHandling = enabled;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of concurrent handlers.
    /// </summary>
    /// <param name="maxConcurrent">The maximum number of handlers that may run at the same time. Must be at least 1.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxConcurrent"/> is less than 1.</exception>
    public EventBusBuilder WithMaxConcurrentHandlers(int maxConcurrent)
    {
        if (maxConcurrent < 1)
            throw new ArgumentException("Max concurrent handlers must be at least 1", nameof(maxConcurrent));

        _options.MaxConcurrentHandlers = maxConcurrent;
        return this;
    }

    /// <summary>
    /// Enables or disables the dead letter queue.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to enable the dead letter queue; otherwise <see langword="false"/>.</param>
    /// <returns>This builder, for chaining.</returns>
    public EventBusBuilder WithDeadLetterQueue(bool enabled)
    {
        _options.EnableDeadLetterQueue = enabled;
        return this;
    }

    /// <summary>
    /// Configures whether exceptions from handlers should be thrown or caught.
    /// </summary>
    /// <param name="throwExceptions">
    /// <see langword="true"/> to rethrow handler exceptions; <see langword="false"/> to catch them.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public EventBusBuilder WithThrowOnHandlerFailure(bool throwExceptions)
    {
        _options.ThrowOnHandlerFailure = throwExceptions;
        return this;
    }

    /// <summary>
    /// Configures whether publishing an event with no registered handlers throws
    /// <see cref="Exceptions.NoHandlersRegisteredException"/> instead of being logged (and
    /// optionally dead-lettered).
    /// </summary>
    /// <param name="throwExceptions">
    /// <see langword="true"/> to throw <see cref="Exceptions.NoHandlersRegisteredException"/> when no handlers
    /// are registered; <see langword="false"/> to log the event instead.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public EventBusBuilder WithThrowOnNoHandlers(bool throwExceptions)
    {
        _options.ThrowOnNoHandlers = throwExceptions;
        return this;
    }

    /// <summary>
    /// Configures whether events published with no registered handlers are recorded in the
    /// dead letter queue instead of being silently logged.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to dead-letter events that have no registered handlers; otherwise <see langword="false"/>.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    public EventBusBuilder WithDeadLetterOnNoHandlers(bool enabled)
    {
        _options.DeadLetterOnNoHandlers = enabled;
        return this;
    }

    /// <summary>
    /// Configures distributed event bus settings.
    /// </summary>
    /// <param name="transportType">The identifier of the distributed transport to use. Cannot be null, empty, or whitespace.</param>
    /// <param name="connectionString">
    /// An optional connection string for the distributed transport, or <see langword="null"/> if the transport
    /// does not require one.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="transportType"/> is null, empty, or whitespace.</exception>
    public EventBusBuilder AsDistributed(string transportType, string? connectionString = null)
    {
        if (string.IsNullOrWhiteSpace(transportType))
            throw new ArgumentException("Transport type cannot be empty", nameof(transportType));

        _options.IsDistributed = true;
        _options.DistributedTransportType = transportType;
        _options.DistributedTransportConnectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Builds the event bus and registers it in the service collection.
    /// </summary>
    /// <returns>The service collection that the event bus was registered in.</returns>
    /// <exception cref="DotnetEventBus.Exceptions.ValidationException">Thrown when the configured options are invalid.</exception>
    public IServiceCollection Build()
    {
        _options.Validate();

        if (_messageRepository is not null || _subscriptionRepository is not null || _deadLetterRepository is not null)
        {
            _messageRepository ??= new InMemoryEventMessageRepository();
            _subscriptionRepository ??= new InMemorySubscriptionRepository();
            _deadLetterRepository ??= new InMemoryDeadLetterRepository(_options.MaxDeadLetterEntries);

            _services.AddEventBus(
                _messageRepository,
                _subscriptionRepository,
                _deadLetterRepository,
                opt =>
                {
                    opt.DefaultHandlerTimeout = _options.DefaultHandlerTimeout;
                    opt.MaxRetryAttempts = _options.MaxRetryAttempts;
                    opt.RetryDelay = _options.RetryDelay;
                    opt.RetryDelayMultiplier = _options.RetryDelayMultiplier;
                    opt.MaxRetryDelay = _options.MaxRetryDelay;
                    opt.AllowParallelHandling = _options.AllowParallelHandling;
                    opt.MaxConcurrentHandlers = _options.MaxConcurrentHandlers;
                    opt.EnableDeadLetterQueue = _options.EnableDeadLetterQueue;
                    opt.ThrowOnHandlerFailure = _options.ThrowOnHandlerFailure;
                    opt.ThrowOnNoHandlers = _options.ThrowOnNoHandlers;
                    opt.DeadLetterOnNoHandlers = _options.DeadLetterOnNoHandlers;
                    opt.MaxDeadLetterEntries = _options.MaxDeadLetterEntries;
                    opt.IsDistributed = _options.IsDistributed;
                    opt.DistributedTransportType = _options.DistributedTransportType;
                    opt.DistributedTransportConnectionString = _options.DistributedTransportConnectionString;
                });
        }
        else
        {
            _services.AddEventBus(opt =>
            {
                opt.DefaultHandlerTimeout = _options.DefaultHandlerTimeout;
                opt.MaxRetryAttempts = _options.MaxRetryAttempts;
                opt.RetryDelay = _options.RetryDelay;
                opt.RetryDelayMultiplier = _options.RetryDelayMultiplier;
                opt.MaxRetryDelay = _options.MaxRetryDelay;
                opt.AllowParallelHandling = _options.AllowParallelHandling;
                opt.MaxConcurrentHandlers = _options.MaxConcurrentHandlers;
                opt.EnableDeadLetterQueue = _options.EnableDeadLetterQueue;
                opt.ThrowOnHandlerFailure = _options.ThrowOnHandlerFailure;
                opt.ThrowOnNoHandlers = _options.ThrowOnNoHandlers;
                opt.DeadLetterOnNoHandlers = _options.DeadLetterOnNoHandlers;
                opt.MaxDeadLetterEntries = _options.MaxDeadLetterEntries;
                opt.IsDistributed = _options.IsDistributed;
                opt.DistributedTransportType = _options.DistributedTransportType;
                opt.DistributedTransportConnectionString = _options.DistributedTransportConnectionString;
            });
        }

        return _services;
    }
}

/// <summary>
/// Extension methods for EventBusBuilder.
/// </summary>
public static class EventBusBuilderExtensions
{
    /// <summary>
    /// Creates and configures a new EventBusBuilder.
    /// </summary>
    /// <param name="services">The service collection the event bus will be registered in.</param>
    /// <returns>A new <see cref="EventBusBuilder"/> bound to <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static EventBusBuilder AddEventBusBuilder(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return new EventBusBuilder(services);
    }
}
