# RabbitMqOptions

`RabbitMqOptions` holds the connection settings for the RabbitMQ overload of `AddEventBus`.

> **Status:** no RabbitMQ bus is implemented yet. The overload validates its input and then throws `NotSupportedException`, so it cannot be used to deliver messages today. See the README section "Using RabbitMQ".

## Properties

### `public string ConnectionString { get; set; }`

The AMQP connection string, for example `amqp://user:password@host:5672/`. Defaults to an empty string, which is rejected at registration.

## Registration

### `public static IServiceCollection AddEventBus(this IServiceCollection services, RabbitMqOptions? options)`

- **Throws** `InvalidOperationException` if `options` is `null`, if `ConnectionString` is empty or whitespace, or if an in-memory bus is already registered. Each message names the problem.
- **Throws** `NotSupportedException` when the options are valid. Nothing is registered in that case.

Only one bus implementation may be registered per `IServiceCollection`. Calling `AddEventBus()` (in-memory) again, or after `AddInProcessTransport()`, is allowed.
