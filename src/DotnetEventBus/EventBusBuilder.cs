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
    public EventBusBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Configures the event bus options.
    /// </summary>
    public EventBusBuilder WithOptions(Action<EventBusOptions> configureOptions)
    {
        if (configureOptions is null)
            throw new ArgumentNullException(nameof(configureOptions));

        configureOptions(_options);
        return this;
    }

    /// <summary>
    /// Sets a custom event message repository.
    /// </summary>
    public EventBusBuilder WithMessageRepository(IEventMessageRepository repository)
    {
        _messageRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        return this;
    }

    /// <summary>
    /// Sets a custom subscription repository.
    /// </summary>
    public EventBusBuilder WithSubscriptionRepository(ISubscriptionRepository repository)
    {
        _subscriptionRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        return this;
    }

    /// <summary>
    /// Sets a custom dead letter repository.
    /// </summary>
    public EventBusBuilder WithDeadLetterRepository(IDeadLetterRepository repository)
    {
        _deadLetterRepository = repository ?? throw new ArgumentNullException(nameof(repository));
        return this;
    }

    /// <summary>
    /// Sets the maximum retry attempts.
    /// </summary>
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
    public EventBusBuilder WithParallelHandling(bool enabled)
    {
        _options.AllowParallelHandling = enabled;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of concurrent handlers.
    /// </summary>
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
    public EventBusBuilder WithDeadLetterQueue(bool enabled)
    {
        _options.EnableDeadLetterQueue = enabled;
        return this;
    }

    /// <summary>
    /// Configures whether exceptions from handlers should be thrown or caught.
    /// </summary>
    public EventBusBuilder WithThrowOnHandlerFailure(bool throwExceptions)
    {
        _options.ThrowOnHandlerFailure = throwExceptions;
        return this;
    }

    /// <summary>
    /// Configures distributed event bus settings.
    /// </summary>
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
    public IServiceCollection Build()
    {
        _options.Validate();

        if (_messageRepository is not null || _subscriptionRepository is not null || _deadLetterRepository is not null)
        {
            _messageRepository ??= new InMemoryEventMessageRepository();
            _subscriptionRepository ??= new InMemorySubscriptionRepository();
            _deadLetterRepository ??= new InMemoryDeadLetterRepository();

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
    public static EventBusBuilder AddEventBusBuilder(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        return new EventBusBuilder(services);
    }
}
