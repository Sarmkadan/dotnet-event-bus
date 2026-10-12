# RabbitMqOptions

`RabbitMqOptions` holds the connection settings for the RabbitMQ overload of `AddEventBus`.

> **Status:** no RabbitMQ bus is implemented yet. The overload validates its input and then throws `NotSupportedException`, so it cannot be used to deliver messages today. See the README section "Using RabbitMQ".

## Properties

### `public string ConnectionString { get; set; }`

The AMQP connection string, for example `amqp://user:password@host:5672/`. Defaults to an empty string, which is rejected at registration.

### `public string HostName { get; set; }`

Broker host name. Defaults to `localhost`. Must not be empty.

### `public int Port { get; set; }`

Broker port. Defaults to `5672`. Must be between 1 and 65535.

### `public string ExchangeName { get; set; }`

Name of the exchange events are published to. Defaults to `dotnet-event-bus`. Must not be empty.

### `public int PrefetchCount { get; set; }`

Maximum number of unacknowledged messages a consumer may hold. Defaults to `10`. Must be greater than zero.

### `public int MaxRetryAttempts { get; set; }`

Maximum number of retries for a failed message. Defaults to `3`. Cannot be negative.

### `public TimeSpan RetryDelay { get; set; }`

Base delay for exponential backoff. Defaults to 100 ms. Cannot be negative.

### `public double RetryDelayMultiplier { get; set; }`

Multiplier applied to the delay on each attempt. Defaults to `2.0`. Must be at least `1.0`.

### `public TimeSpan MaxRetryDelay { get; set; }`

Upper bound for any single retry delay. Defaults to 30 s. Must not be less than `RetryDelay`.

## Validation

`RabbitMqOptionsValidator` (`IValidateOptions<RabbitMqOptions>`) checks the properties above and reports every failure, each naming the property. The RabbitMQ overload of `AddEventBus` runs it eagerly, so bad settings fail during startup rather than at the first publish. Callers that bind configuration through the Options pattern can register it with `services.AddSingleton<IValidateOptions<RabbitMqOptions>, RabbitMqOptionsValidator>()`.

## Registration

### `public static IServiceCollection AddEventBus(this IServiceCollection services, RabbitMqOptions? options)`

- **Throws** `InvalidOperationException` if `options` is `null`, if `ConnectionString` is empty or whitespace, or if an in-memory bus is already registered. Each message names the problem.
- **Throws** `OptionsValidationException` if any property fails validation. The message names each invalid property.
- **Throws** `NotSupportedException` when the options are valid. Nothing is registered in that case.

Only one bus implementation may be registered per `IServiceCollection`. Calling `AddEventBus()` (in-memory) again, or after `AddInProcessTransport()`, is allowed.
