See [CONTRIBUTING.md](CONTRIBUTING.md) for build, test, and pull request guidelines.

## Getting started

This walkthrough builds a small ASP.NET Core API that publishes an `OrderPlacedEvent` from a controller and handles it in a handler class. It targets .NET 10.

Install the package (or add a project reference to `src/DotnetEventBus`):

```bash
dotnet add package Zaiets.dotnet.event.bus
```

### 1. Define an event

Events are plain classes or records. Any reference type can be an event.

```csharp
// Events/OrderPlacedEvent.cs
namespace MyApp.Events;

public sealed record OrderPlacedEvent(string OrderId, string CustomerId, decimal Total, DateTime PlacedAt);
```

### 2. Create a handler class

Handlers derive from `EventHandlerBase<TEvent>` and override `Handle`. The optional `ILogger` constructor argument is passed to the base class.

```csharp
// Handlers/OrderPlacedHandler.cs
using DotnetEventBus.Handlers;
using Microsoft.Extensions.Logging;
using MyApp.Events;

namespace MyApp.Handlers;

public sealed class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : EventHandlerBase<OrderPlacedEvent>(logger)
{
    public override Task Handle(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation("Order {OrderId} placed for {Total}", @event.OrderId, @event.Total);
        return Task.CompletedTask;
    }
}
```

### 3. Register the bus and subscribe

`AddEventBus()` registers `IEventBus` as a singleton with default options (pass a callback to customize `EventBusOptions`). Handlers are not discovered from DI automatically, so subscribe each one once at startup.

```csharp
// Program.cs
using DotnetEventBus.Configuration;
using DotnetEventBus.Services;
using MyApp.Events;
using MyApp.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEventBus();
builder.Services.AddSingleton<OrderPlacedHandler>();

var app = builder.Build();

var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<OrderPlacedEvent>(app.Services.GetRequiredService<OrderPlacedHandler>());

app.MapControllers();
app.Run();
```

### 4. Publish from a controller

Inject `IEventBus` like any other service. `PublishAsync` returns a `PublishResult`. By default, handler exceptions are logged and reported as a failed result rather than thrown, so check `Success`.

```csharp
// Controllers/OrdersController.cs
using DotnetEventBus.Services;
using Microsoft.AspNetCore.Mvc;
using MyApp.Events;

namespace MyApp.Controllers;

public sealed record PlaceOrderRequest(string CustomerId, decimal Total);

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IEventBus eventBus) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid().ToString("N");
        var @event = new OrderPlacedEvent(orderId, request.CustomerId, request.Total, DateTime.UtcNow);

        var result = await eventBus.PublishAsync(@event, correlationId: HttpContext.TraceIdentifier, cancellationToken: cancellationToken);

        return result.Success
            ? Accepted(new { orderId, messageId = result.MessageId })
            : Problem(result.ErrorMessage);
    }
}
```

Any injected service can publish the same way. The delegate-based and class-based subscription styles, along with options, are covered in the sections below.

## Using RabbitMQ

> **Status:** the library does not ship a RabbitMQ transport or a `RabbitMqBus` yet. Everything is delivered in-process by `EventBus`, which `AddEventBus()` registers. `AsDistributed(...)` and the `DistributedTransportType` / `DistributedTransportConnectionString` options only store values; nothing connects to a broker from them. The configuration and local broker setup below are what you will use once a RabbitMQ transport exists. They do not connect anything today.

### Configuration

Keep the broker settings in `appsettings.json`. Bind the section to `EventBusOptions` inside the `AddEventBus` callback. The library takes a plain callback rather than `IOptions<T>`, so bind the section there:

```json
{
  "EventBus": {
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:00.100",
    "RetryDelayMultiplier": 2.0,
    "MaxRetryDelay": "00:00:30",
    "EnableDeadLetterQueue": true,
    "IsDistributed": true,
    "DistributedTransportType": "RabbitMQ",
    "DistributedTransportConnectionString": "amqp://localhost:5672/"
  }
}
```

```csharp
// Program.cs
builder.Services.AddEventBus(options =>
    builder.Configuration.GetSection("EventBus").Bind(options));
```

Keep credentials out of source control and `appsettings.json`. Set them through user secrets or an environment variable, which overrides the file value:

```bash
export EventBus__DistributedTransportConnectionString="amqp://eventbus:<password>@localhost:5672/"
```

Validation runs when `AddEventBus` executes, so an invalid section fails at startup. For example, `IsDistributed: true` without `DistributedTransportType` throws.

### Switching from InMemoryBus to RabbitMQ

There is no RabbitMQ bus to switch to yet. Use these options today:

| Scenario | Use |
|---|---|
| Application code, single process | `AddEventBus()` (registers `EventBus`) |
| Tests or single-process tooling | `new InMemoryBus(logger, options)`, constructed directly. It is not registered by `AddEventBus` and drops payloads that need distributed processing. |
| Delivery across processes through RabbitMQ | Not available. Do not rely on `AsDistributed` for it. |

### Local RabbitMQ with docker-compose

Add this service to your local `docker-compose.yml`. It requires `RABBITMQ_PASSWORD` in the environment or a `.env` file, so no default password is committed:

```yaml
services:
  rabbitmq:
    image: rabbitmq:4-management
    container_name: eventbus-rabbitmq
    ports:
      - "5672:5672"    # AMQP
      - "15672:15672"  # management UI at http://localhost:15672
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:-eventbus}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD:?set RABBITMQ_PASSWORD}
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
```

Start it with `docker compose up -d rabbitmq`. The `guest` account is only allowed from localhost, so the compose file creates a named user instead.

### Health check

`AddEventBusHealthCheck()` registers an ASP.NET Core health check that reads the connection state through `IRabbitMqConnectionMonitor`:

| Connection state (`RabbitMqConnectionState`) | Health status |
|---|---|
| `Open` | `Healthy` |
| `Reconnecting` | `Degraded` |
| `Closed`, or any other value | `Unhealthy` |

The result's `Data` includes `connectionState` with the state name.

```csharp
builder.Services.AddHealthChecks()
    .AddEventBusHealthCheck(tags: ["messaging"]);

app.MapHealthChecks("/health/messaging", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("messaging")
});
```

The check is named `eventbus` by default. Pass a different name as the first argument if you need several buses.

A RabbitMQ bus is responsible for implementing `IRabbitMqConnectionMonitor`. Until one is registered, the check reports `Unhealthy`, because there is no RabbitMQ connection to report on. The library does not register a monitor itself, so no connection state is reported today. The interface is what a future RabbitMQ bus would implement. A custom monitor can be registered as well. The health check reads its `State` on every run:

```csharp
builder.Services.AddSingleton<IRabbitMqConnectionMonitor, MyConnectionMonitor>();
```

### Retries

`EventBus` retries a failing handler using the `EventBusOptions` retry settings:

- `MaxRetryAttempts` is the number of retries after the first failure. With `5`, a handler runs up to six times.
- Before retry `n` (counting from 0), the delay is `RetryDelay × RetryDelayMultiplier^n`, capped at `MaxRetryDelay`. There is no jitter.
- When retries are exhausted, the event goes to the dead letter queue if `EnableDeadLetterQueue` is set and the subscription allows it (`SendToDeadLetterOnFailure`).

```csharp
builder.Services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 5;                       // 6 calls in total
    options.RetryDelay = TimeSpan.FromMilliseconds(200);
    options.RetryDelayMultiplier = 2.0;                 // waits 200 ms, 400 ms, 800 ms, 1.6 s, 3.2 s
    options.MaxRetryDelay = TimeSpan.FromSeconds(5);    // caps each wait
});
```

`RetryHandler` is a standalone helper for wrapping your own calls, such as an HTTP request to an external service. `AddEventBus` does not register it, and `EventBus` does not use it. Register it yourself:

```csharp
builder.Services.AddSingleton<RetryHandler>(); // resolves ILogger<RetryHandler> and EventBusOptions

// In a service that takes RetryHandler as a constructor parameter:
var error = await _retry.ExecuteWithRetryAsync(
    token => CallRemoteServiceAsync(token),
    handlerName: "remote-service",
    cancellationToken: ct);

if (error is not null)
{
    // All attempts failed. The last exception is returned, not thrown.
}
```

`RetryHandler` reads the same options with different rules:

- `MaxRetryAttempts` is the total number of attempts, not retries. A value of `0` or less falls back to 3.
- The delay is `RetryDelay × RetryDelayMultiplier^(attempt − 1)` with ±25% jitter. It is capped at a fixed 30 seconds and ignores `MaxRetryDelay`.

## BasicPubSubExample

The `BasicPubSubExample` class demonstrates the core publish-subscribe pattern of the DotnetEventBus library. It shows how to define custom events, subscribe multiple handlers to those events, and publish events asynchronously. The example includes both delegate-based handlers and class-based handlers, demonstrating different subscription approaches.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Examples;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection with event bus
var services = new ServiceCollection();
services.AddEventBus(options =>
{
    options.AllowParallelHandling = true;
    options.MaxConcurrentHandlers = 4;
    options.EnableDetailedLogging = true;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe handlers using delegate syntax
eventBus.Subscribe<BasicPubSubExample.UserRegisteredEvent>(
    async (@event, ct) =>
    {
        Console.WriteLine($"📊 Recording user registration in analytics");
        await Task.Delay(75);
        Console.WriteLine($"✓ Analytics recorded");
    },
    handlerName: "AnalyticsHandler",
    priority: 0
);

// Subscribe class-based handlers
eventBus.Subscribe<UserRegisteredEvent, SendWelcomeEmailHandler>();
eventBus.Subscribe<UserRegisteredEvent, UpdateUserProfileHandler>();

// Create and publish events
var userRegisteredEvent = new BasicPubSubExample.UserRegisteredEvent
{
    UserId = "user-123",
    Email = "user@example.com",
    FullName = "John Doe",
    RegisteredAt = DateTime.UtcNow
};

var result = await eventBus.PublishAsync(userRegisteredEvent);

Console.WriteLine($"Handlers invoked: {result.HandlersInvoked}");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds:F2}ms");
```

## EventBus

`EventBus` (namespace `DotnetEventBus.Services`) is the in-process implementation of `IEventBus`. It stores subscriptions in memory, publishes events to matching handlers, applies configured middleware and retry behavior, and records publish results. Applications using dependency injection should normally call `AddEventBus` and resolve `IEventBus`; direct construction is available when repositories and the other dependencies need to be supplied explicitly.

Public API:

- `EventBus(EventBusOptions? options = null, ILogger<EventBus>? logger = null, IDeadLetterService? deadLetterService = null, IEventFormatter? eventFormatter = null, IServiceProvider? serviceProvider = null, IEventMessageRepository? messageRepository = null, ISubscriptionRepository? subscriptionRepository = null, IDeadLetterRepository? deadLetterRepository = null)` creates an event bus, using default options, JSON formatting, and private in-memory repositories for omitted values. Despite its optional parameter syntax, `serviceProvider` is required and a null value throws `ArgumentNullException`.
- `EventBus(IEventMessageRepository messageRepository, ISubscriptionRepository subscriptionRepository, IDeadLetterRepository deadLetterRepository, IDeadLetterService deadLetterService, IEventFormatter eventFormatter, IServiceProvider serviceProvider, EventBusOptions? options = null, ILogger<EventBus>? logger = null)` creates an event bus with explicit infrastructure dependencies. Null required dependencies throw `ArgumentNullException`.
- `PublishAsync<TEvent>(TEvent event, string? correlationId = null, CancellationToken cancellationToken = default)` publishes a reference-type event to active handlers registered for its concrete type, base types, or implemented interfaces and returns a `PublishResult`.
- `PublishAsync(object event, Type eventType, string? correlationId = null, CancellationToken cancellationToken = default)` is the runtime-type equivalent used when the event type is known dynamically.
- `Subscribe<TEvent>(IEventHandler<TEvent> handler)` subscribes an `IEventHandler<TEvent>` instance.
- `Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler, string? handlerName = null, int priority = 0)` subscribes an asynchronous delegate. Within each matched event type, higher-priority handlers are selected first when handling is sequential.
- `SubscribeSync<TEvent>(Action<TEvent> handler, string? handlerName = null, int priority = 0)` adapts and subscribes a synchronous delegate.
- Each subscribe method returns an `IDisposable`; disposing it removes that subscription. Keep the returned object alive for as long as the handler should receive events.
- `UnsubscribeAsync(string handlerId, CancellationToken cancellationToken = default)` removes the subscription with the specified subscription ID. An empty ID throws `ArgumentException`.
- `GetSubscriptionsAsync(string eventType, CancellationToken cancellationToken = default)` returns the handler names currently registered for the full event type name. An empty event type throws `ArgumentException`.
- `ClearSubscriptionsAsync(CancellationToken cancellationToken = default)` removes every in-memory subscription.
- `GetOptions()` returns a clone of the current `EventBusOptions`, so changing the returned object does not reconfigure the bus.
- `ProcessRawDistributedEventAsync(string eventType, string rawPayload, string? correlationId = null, CancellationToken cancellationToken = default)` resolves an assembly-qualified type name, deserializes the payload, and publishes it. Unknown types and deserialization failures are sent to the dead-letter service and returned as failed publish results.
- `SendAsync<TRequest, TResponse>(TRequest request, string? correlationId = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)` and `SubscribeRequest<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)` reserve the request/reply API. Request/reply requires distributed transport configuration and its transport implementation is not currently available; these methods throw `ConfigurationException` or `NotImplementedException` rather than processing a request.

Publish/subscribe example:

```csharp
using DotnetEventBus.Configuration;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEventBus(options =>
{
    options.AllowParallelHandling = false;
    options.ThrowOnHandlerFailure = true;
});

await using var provider = services.BuildServiceProvider();
var eventBus = provider.GetRequiredService<IEventBus>();

// Dispose the subscription when this handler should stop receiving events.
using var subscription = eventBus.Subscribe<OrderCreated>(
    async (order, cancellationToken) =>
    {
        Console.WriteLine($"Processing order {order.OrderId}");
        await Task.CompletedTask;
    },
    handlerName: "OrderCreatedHandler",
    priority: 10);

var result = await eventBus.PublishAsync(
    new OrderCreated(123),
    correlationId: "order-123");

Console.WriteLine(
    $"Invoked: {result.HandlersInvoked}; failed: {result.FailedHandlers}");

public sealed record OrderCreated(int OrderId);
```

## EventFormatterFactory

`EventFormatterFactory` (`src/DotnetEventBus/Formatters/EventFormatterFactory.cs`) is a registry of `IEventFormatter` implementations. Formatters are stored under their `Format` name (for example `json`), and can be looked up by that name or by MIME content type. Registering a formatter under a name that is already in use replaces the previous one.

`CreateDefault()` registers the built-in formatters:

| Format | Class | Content type |
|--------|-------|--------------|
| `json` | `JsonEventFormatter` | `application/json` |
| `csv` | `CsvEventFormatter` | `text/csv` |
| `xml` | `XmlEventFormatter` | `application/xml` |

### Public API

| Member | Description |
|--------|-------------|
| `static EventFormatterFactory CreateDefault()` | Creates a new factory with the JSON, CSV and XML formatters registered. |
| `void Register(IEventFormatter formatter)` | Adds the formatter under `formatter.Format` (case-insensitive), replacing any existing formatter with that name. Throws `ArgumentNullException` if `formatter` is `null`. |
| `IEventFormatter? GetFormatter(string format)` | Returns the formatter registered under the format name (case-insensitive), or `null` if the name is empty or unknown. |
| `IEventFormatter? GetFormatterByContentType(string contentType)` | Returns the formatter whose `ContentType` matches exactly (case-insensitive). If none matches, it uses the subtype with parameters removed as a format name, so `application/json; charset=utf-8` resolves to `json`. Returns `null` if the value is empty or nothing matches. |
| `IEnumerable<IEventFormatter> GetAllFormatters()` | Returns a snapshot of all registered formatters. |
| `IEnumerable<string> GetSupportedFormats()` | Returns a snapshot of the registered format names, in lowercase. |
| `bool IsFormatSupported(string format)` | Returns `true` if a formatter is registered under the format name (case-insensitive). |
| `bool Unregister(string format)` | Removes the formatter registered under the format name (case-insensitive). Returns `true` if one was removed. |

Notes:

- `AddEventBus(...)` registers `EventFormatterFactory` as an empty singleton. It does not call `CreateDefault()`. Register the formatters you need, or create the factory with `CreateDefault()` yourself.
- The factory is not thread-safe. Register and unregister formatters during startup, before the factory is shared across threads. Concurrent reads without writes are fine.

### Usage

Create a factory with the built-in formatters and resolve one by name or content type:

```csharp
using DotnetEventBus.Formatters;

var factory = EventFormatterFactory.CreateDefault();

if (factory.GetFormatter("JSON") is { } json)
{
    var payload = json.Serialize(new OrderCreated(123, 99.99m), prettyPrint: true);
    var copy = json.Deserialize<OrderCreated>(payload);
}

// Parameters in the content type are ignored
var csv = factory.GetFormatterByContentType("text/csv");

public sealed record OrderCreated(int OrderId, decimal Amount);
```

Register a custom formatter. Implement `IEventFormatter`, which requires `Format`, `ContentType`, `Serialize`, `Deserialize<T>`, `Deserialize(string, Type)`, `FormatEvent` and `FormatEventWithMetadata`:

```csharp
var factory = new EventFormatterFactory();

factory.Register(new JsonEventFormatter());   // empty factory: add the built-in JSON formatter
factory.Register(new MyProtobufFormatter());  // Format = "protobuf", ContentType = "application/x-protobuf"

factory.IsFormatSupported("protobuf");        // true
factory.Unregister("protobuf");               // true
```

Inspect what is registered:

```csharp
IEnumerable<string> names = factory.GetSupportedFormats();   // e.g. "json", "csv", "xml"

foreach (var formatter in factory.GetAllFormatters())
{
    Console.WriteLine($"{formatter.Format} -> {formatter.ContentType}");
}
```

### Validation

`EventFormatterFactoryValidation` provides extension methods for checking a factory's configuration:

| Member | Description |
|--------|-------------|
| `IReadOnlyList<string> Validate()` | Returns the list of problems, empty if the factory is valid. Throws `ArgumentNullException` for a `null` factory. |
| `bool IsValid()` | Returns `true` if the factory has no problems. Returns `false` for a `null` factory. |
| `void EnsureValid()` | Throws `ArgumentException` listing the problems. Throws `ArgumentNullException` for a `null` factory. |

A factory is reported as invalid when it has no formatters, when a formatter has a blank `Format` or `ContentType`, when a format name contains whitespace or control characters, or when two formatters share a content type (case-insensitive).

```csharp
using DotnetEventBus.Formatters;

var factory = EventFormatterFactory.CreateDefault();

if (!factory.IsValid())
{
    foreach (var problem in factory.Validate())
    {
        Console.WriteLine(problem);
    }
}

// Or fail fast at startup
factory.EnsureValid();
```

## XmlEventFormatter

`XmlEventFormatter` (`src/DotnetEventBus/Formatters/XmlEventFormatter.cs`) implements `IEventFormatter` and converts events to and from XML for legacy and enterprise integrations. It is built on `System.Xml.Serialization.XmlSerializer`, so event types must follow the rules in [Event type requirements](#event-type-requirements).

Format name `xml`, content type `application/xml`.

### Constructor

```csharp
public XmlEventFormatter(bool omitXmlDeclaration = false, Encoding? encoding = null)
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `omitXmlDeclaration` | `false` | When `true`, output has no `<?xml ... ?>` declaration line. Set it for consumers that reject the declaration. |
| `encoding` | UTF-8 without byte-order mark | Encoding used to write the XML. The declaration names it, for example `encoding="utf-8"`. |

### Public API

| Member | Description |
|--------|-------------|
| `string Format` | Always `"xml"`. |
| `string ContentType` | Always `"application/xml"`. |
| `string Serialize(object data, bool prettyPrint = false)` | Returns `data` as an XML document whose root element is named after the runtime type of `data`. `prettyPrint` adds indentation. Throws `ArgumentNullException` if `data` is `null`, and `InvalidOperationException` if `XmlSerializer` cannot handle the type. |
| `T? Deserialize<T>(string data) where T : class` | Returns the parsed object. Returns `null` if `data` is `null` or empty, and for any other failure. It does not throw. |
| `object? Deserialize(string data, Type targetType)` | Non-generic version of the method above, with the same rules. A `null` `targetType` also returns `null` instead of throwing. |
| `string FormatEvent(object eventData, bool includePrettyPrint = false)` | Same output as `Serialize`. |
| `string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false)` | Returns an `<Event>` envelope that holds the event and its metadata. See [Output rules](#output-rules). |

Notes:

- `Deserialize` returns `null` for every failure, including malformed XML and a root element that does not match `T`. It cannot tell you why the input failed, so check for `null` and handle it.
- The root element must match the target type. `<Event>` envelopes from `FormatEventWithMetadata` are not read back as the event. Pull out the `<Data>` element first.
- The formatter holds no mutable state after construction, so one instance can be shared across threads.

### Event type requirements

`XmlSerializer` needs the event type to be:

- A public class or struct with a public parameterless constructor. Anonymous types and records with primary-constructor parameters throw `InvalidOperationException`.
- Made of public read/write properties. Each one becomes a child element. Fields and non-public members are not written.

Element names are the type and property names. No `xmlns` attributes are written. Values use invariant culture, so `299.99m` is always written as `299.99`.

### Output rules

- Each `Serialize` call returns one complete document with one root element.
- With the default `prettyPrint: false`, the document is on one line with no whitespace between elements. With `prettyPrint: true`, elements are indented with two spaces.
- Special characters are escaped, so `&` becomes `&amp;` and `<` becomes `&lt;`.
- `FormatEventWithMetadata` writes this structure:
  - `<Event timestamp="...">` contains the UTC time of formatting in ISO 8601 round-trip format (`"o"`).
  - `<Data>` holds the event serialized with its XML declaration removed. The event is always written compactly, so `includePrettyPrint` only indents the envelope.
  - `<Metadata>` has one child element per entry. The value is written with `ToString()`, and a `null` value becomes an empty element. An empty `<Metadata></Metadata>` is written when there are no entries.
  - Each metadata key is sanitized into a valid element name. Any character outside `A-Z`, `a-z`, `0-9`, `_` and `-` becomes `_`, and a leading digit gets a `_` prefix. For example, `"Retry Count"` becomes `Retry_Count` and `"1st"` becomes `_1st`. Two keys that sanitize to the same name produce duplicate elements. An empty key throws `IndexOutOfRangeException`.
- `FormatEventWithMetadata` throws `NullReferenceException` when `metadata` is `null`. Use `Serialize` if you want an `ArgumentNullException` check.
- A UTF-16 `encoding` writes a byte-order mark, which appears as `﻿` at the start of the returned string.

### Usage

```csharp
using System.Text;
using DotnetEventBus.Formatters;

// Parameterless constructor and public read/write properties
public sealed class OrderPlaced
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? CustomerName { get; set; }
}

var formatter = new XmlEventFormatter();
var order = new OrderPlaced { OrderId = "ORD-123", Total = 299.99m, CustomerName = "Alice & Bob" };

string xml = formatter.Serialize(order, prettyPrint: true);
```

```xml
<?xml version="1.0" encoding="utf-8"?>
<OrderPlaced>
  <OrderId>ORD-123</OrderId>
  <Total>299.99</Total>
  <CustomerName>Alice &amp; Bob</CustomerName>
</OrderPlaced>
```

Deserialize the document back. Check for `null` to detect bad input:

```csharp
OrderPlaced? parsed = formatter.Deserialize<OrderPlaced>(xml);

if (parsed is null)
{
    // Empty, malformed, or a different root element
}

// Non-generic version
object? untyped = formatter.Deserialize(xml, typeof(OrderPlaced));
```

Omit the declaration for a consumer that rejects it:

```csharp
var legacy = new XmlEventFormatter(omitXmlDeclaration: true);
string compact = legacy.Serialize(order);

// <OrderPlaced><OrderId>ORD-123</OrderId><Total>299.99</Total><CustomerName>Alice &amp; Bob</CustomerName></OrderPlaced>
```

Wrap the event with metadata:

```csharp
var metadata = new Dictionary<string, object>
{
    ["Source"] = "checkout-api",
    ["Retry Count"] = 2,          // element name becomes Retry_Count
};

string envelope = formatter.FormatEventWithMetadata(order, metadata, includePrettyPrint: true);
```

```xml
<?xml version="1.0" encoding="utf-8"?>
<Event timestamp="2026-10-25T12:00:00.1234567Z">
  <Data><OrderPlaced><OrderId>ORD-123</OrderId><Total>299.99</Total><CustomerName>Alice &amp; Bob</CustomerName></OrderPlaced></Data>
  <Metadata>
    <Source>checkout-api</Source>
    <Retry_Count>2</Retry_Count>
  </Metadata>
</Event>
```

Get the default XML formatter from the factory:

```csharp
var xmlFormatter = EventFormatterFactory.CreateDefault().GetFormatter("xml");
```

## JsonEventFormatter
The `JsonEventFormatter` class formats events as JSON strings for serialization and API responses. It supports both compact and pretty-printed output, and provides methods for serializing objects to JSON, deserializing JSON strings to objects, and formatting events with or without metadata.

Example usage:
```csharp
using DotnetEventBus.Formatters;
using System;
using System.Collections.Generic;

// Create a new JSON formatter
var formatter = new JsonEventFormatter();

// Serialize an object to compact JSON
var eventData = new { Id = 1, Name = "Test Event", Timestamp = DateTime.UtcNow };
string compactJson = formatter.Serialize(eventData);

// Serialize with pretty printing
string prettyJson = formatter.Serialize(eventData, prettyPrint: true);

// Deserialize JSON back to a strongly-typed object
var deserializedData = formatter.Deserialize<Dictionary<string, object>>(compactJson);

// Format an event as JSON
string formattedEvent = formatter.FormatEvent(eventData);

// Format an event with metadata
var metadata = new Dictionary<string, object> {
    { "source", "event-bus" },
    { "priority", "high" }
};
string formattedEventWithMetadata = formatter.FormatEventWithMetadata(eventData, metadata);

// Deserialize to a specific type
var typedData = formatter.Deserialize<MyEventType>(compactJson);

public class MyEventType
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## CsvEventFormatter

`CsvEventFormatter` (`src/DotnetEventBus/Formatters/CsvEventFormatter.cs`) implements `IEventFormatter` and writes events as CSV for bulk export and reporting. It is serialization only: `Deserialize` throws `NotSupportedException`. Columns come from the public, readable instance properties of the event's runtime type, so anonymous types, records and classes all work.

Format name `csv`, content type `text/csv`.

### Constructor

```csharp
public CsvEventFormatter(string delimiter = ",", bool includeHeaders = true)
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `delimiter` | `","` | Separator between fields. Use a non-empty value; it is not validated. |
| `includeHeaders` | `true` | When `true`, output starts with a header row of property names (and metadata keys). |

### Public API

| Member | Description |
|--------|-------------|
| `string Format` | Always `"csv"`. |
| `string ContentType` | Always `"text/csv"`. |
| `string Delimiter` | The delimiter passed to the constructor. |
| `bool IncludeHeaders` | Whether a header row is written. |
| `string Serialize(object data, bool prettyPrint = false)` | Returns the header row (if enabled) and one value row for `data`. `prettyPrint` has no effect. Throws `ArgumentNullException` if `data` is `null`. |
| `string FormatEvent(object eventData, bool includePrettyPrint = false)` | Same output as `Serialize`. `includePrettyPrint` has no effect. |
| `string FormatEventWithMetadata(object eventData, Dictionary<string, object> metadata, bool includePrettyPrint = false)` | Returns the event's columns followed by one column per metadata entry. Metadata keys are appended to the header row. `includePrettyPrint` has no effect. |
| `T? Deserialize<T>(string data) where T : class` | Always throws `NotSupportedException`. |
| `object? Deserialize(string data, Type targetType)` | Always throws `NotSupportedException`. |

### Output rules

- Each line, header and value, ends with a newline.
- Each call writes exactly one value row. To export many events into one file, create the formatter with `includeHeaders: false` for the rows after the first, or write the header once yourself.
- A `null` property value or metadata value is written as an empty field.
- Values are written with `ToString()`, so numbers and dates use the current culture. In a culture that uses `,` as the decimal separator, `299.99m` becomes `299,99` and is quoted when the delimiter is `,`.
- A field is wrapped in double quotes when it contains the delimiter, a double quote or a `\n`. Embedded double quotes are doubled. A bare `\r` does not trigger quoting.
- A property whose getter throws is written as an empty field.
- Only public, instance properties with a getter are included. Write-only and non-public properties are skipped.
- `FormatEventWithMetadata` does not check its arguments for `null`. Use `Serialize` when you want the `ArgumentNullException` check.

### Usage

```csharp
using DotnetEventBus.Formatters;

public sealed record OrderPlaced(string OrderId, decimal Total, string CustomerName);

var formatter = new CsvEventFormatter();
string csv = formatter.Serialize(new OrderPlaced("ORD-123", 299.99m, "Alice, Jr."));

// OrderId,Total,CustomerName
// ORD-123,299.99,"Alice, Jr."
```

Export rows with a custom delimiter and no header:

```csharp
var semicolon = new CsvEventFormatter(delimiter: ";", includeHeaders: false);
string row = semicolon.Serialize(new OrderPlaced("ORD-124", 5m, "Bob"));

// ORD-124;5;Bob
```

Add metadata columns:

```csharp
var metadata = new Dictionary<string, object>
{
    ["EventType"] = nameof(OrderPlaced),
    ["ReceivedAt"] = DateTimeOffset.UtcNow.ToString("O"),
};

string line = formatter.FormatEventWithMetadata(new OrderPlaced("ORD-123", 299.99m, "Alice, Jr."), metadata);

// OrderId,Total,CustomerName,EventType,ReceivedAt
// ORD-123,299.99,"Alice, Jr.",OrderPlaced,2026-10-25T12:00:00.0000000+00:00
```

Get the default CSV formatter from the factory:

```csharp
var csvFormatter = EventFormatterFactory.CreateDefault().GetFormatter("csv");
```

## DeadLetterService

`DeadLetterService` (namespace `DotnetEventBus.Services`) implements `IDeadLetterService` and manages entries that could not be processed normally. `AddEventBus` registers the service, its repository, and the event bus as singletons, so applications using dependency injection should normally resolve `IDeadLetterService` instead of constructing the implementation directly. Every asynchronous operation accepts an optional `CancellationToken`.

Public API:

- `DeadLetterService(IDeadLetterRepository repository, IEventBus? eventBus = null, ILogger<DeadLetterService>? logger = null)` creates a service for direct use. Reprocessing requires a non-null event bus.
- `DeadLetterService(IDeadLetterRepository repository, IServiceProvider serviceProvider, ILogger<DeadLetterService>? logger = null)` creates a service that resolves `IEventBus` lazily; this is the constructor used by `AddEventBus`. Both constructors throw `ArgumentNullException` when a required argument is null.
- `GetPendingEntriesAsync(...)` returns all entries whose status is `Pending`.
- `GetEntriesByEventTypeAsync(string eventType, ...)` and `GetEntriesByHandlerAsync(string handlerName, ...)` query entries by event type or failed handler. A null, empty, or whitespace query throws `ArgumentException`.
- `ReprocessEntryAsync(string entryId, ...)` publishes the stored payload through the event bus and updates the entry to `Reprocessed` or `ReprocessFailed`. It returns `false` when the entry is missing, no event bus is available, publishing fails, or publishing throws. An empty entry ID throws `ArgumentException`.
- `ReprocessByEventTypeAsync(string eventType, int? maxEntries = null, ...)` reprocesses pending entries of one event type, optionally limiting the number attempted. One failure does not stop the remaining entries.
- `ReplayAsync(Func<DeadLetterEntry, bool> predicate, int? maxEntries = null, ...)` applies a predicate to pending entries and reprocesses the matches, optionally limiting the number attempted. A null predicate throws `ArgumentNullException`.
- `MarkAsReviewedAsync(string entryId, string? reason = null, ...)` changes an entry to `ReviewedNotProcessed` and stores the optional reason. It throws `InvalidOperationException` if the entry does not exist.
- `ArchiveOldEntriesAsync(TimeSpan retentionPeriod, ...)` archives entries older than the retention period and returns the number archived. A zero or negative period throws `ArgumentException`.
- `GetStatisticsAsync(...)` returns status totals and counts grouped by event type and handler.
- `PurgeAsync(...)` permanently clears every dead letter entry from the repository.
- `AddDeadLetterEntryAsync(string eventType, string rawPayload, Exception exception, string? correlationId = null, string? handlerName = null, ...)` stores an external payload that could not be deserialized. The new entry is pending, has distributed scope, uses `DeserializationFailed` when no handler name is supplied, and records zero retry attempts. Empty event types or payloads throw `ArgumentException`; a null exception throws `ArgumentNullException`.

Batch and statistics results:

- `BatchReprocessResult` exposes mutable `SucceededCount`, `FailedCount`, and `FailedEntryIds` values. `TotalAttempted` is their count sum, and `AllSucceeded` is true when `FailedCount` is zero (including an empty batch).
- `DeadLetterStatistics` exposes `TotalEntries`, counts for `PendingEntries`, `ReviewedNotProcessedEntries`, `ReprocessedEntries`, `ReprocessFailedEntries`, and `ArchivedEntries`, plus the `EntriesByEventType` and `EntriesByHandler` dictionaries.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEventBus(options => options.EnableDeadLetterQueue = true);

await using var provider = services.BuildServiceProvider();
var deadLetters = provider.GetRequiredService<IDeadLetterService>();

// Record a payload that could not be deserialized.
await deadLetters.AddDeadLetterEntryAsync(
    eventType: typeof(OrderCreated).AssemblyQualifiedName!,
    rawPayload: "{ \"orderId\": 123 }",
    exception: new InvalidOperationException("Invalid order payload"),
    correlationId: "order-123");

var pending = (await deadLetters.GetPendingEntriesAsync()).ToList();
foreach (var entry in pending)
    Console.WriteLine(entry.GetSummary());

// Replay selected entries; each failure is reported without stopping the batch.
BatchReprocessResult result = await deadLetters.ReplayAsync(
    entry => entry.Message.CorrelationId == "order-123",
    maxEntries: 10);

Console.WriteLine($"Attempted: {result.TotalAttempted}, failed: {result.FailedCount}");

DeadLetterStatistics statistics = await deadLetters.GetStatisticsAsync();
Console.WriteLine($"Pending: {statistics.PendingEntries} of {statistics.TotalEntries}");

public sealed record OrderCreated(int OrderId);
```

## SubscriptionManager

`SubscriptionManager` (namespace `DotnetEventBus.Services`) implements `ISubscriptionManager` and provides a read and control layer over an `ISubscriptionRepository`. `AddEventBus` registers both services as singletons, so applications using dependency injection should normally resolve `ISubscriptionManager`. Every asynchronous operation accepts an optional `CancellationToken`.

Public API:

- `SubscriptionManager(ISubscriptionRepository repository, ILogger<SubscriptionManager>? logger = null)` creates a manager over the supplied repository with optional diagnostic logging. A null repository throws `ArgumentNullException`.
- `GetSubscriptionsAsync(string eventType, ...)` returns snapshots of the subscriptions for one event type, ordered from highest to lowest priority. A null, empty, or whitespace event type throws `ArgumentException`.
- `GetAllSubscriptionsAsync(...)` returns snapshots of every subscription in repository order.
- `GetSubscriptionCountAsync(string eventType, ...)` returns the number of subscriptions for one event type. A null, empty, or whitespace event type throws `ArgumentException`.
- `DisableHandlerAsync(string handlerName, ...)` disables every subscription with the supplied handler name.
- `EnableHandlerAsync(string handlerName, ...)` enables every subscription with the supplied handler name.
- Both handler control methods throw `ArgumentException` for a null, empty, or whitespace handler name. If no matching subscriptions exist, they return without changing the repository.
- `GetStatisticsAsync(...)` returns totals and grouped counts for the repository's current subscription state.

Returned models:

- `SubscriptionInfo` is a mutable snapshot exposing `Id`, `EventType`, `HandlerName`, `IsActive`, `Priority`, `IsAsync`, optional `Timeout`, and `CreatedAtUtc`. Changing the snapshot does not update the stored subscription.
- `SubscriptionStatistics` exposes `TotalSubscriptions`, `ActiveSubscriptions`, `InactiveSubscriptions`, `UniqueEventTypes`, and `UniqueHandlers`. Its `SubscriptionsByEventType`, `SubscriptionsByHandler`, and `ActiveSubscriptionsByEventType` dictionaries contain the corresponding grouped counts.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEventBus();

await using var provider = services.BuildServiceProvider();
var repository = provider.GetRequiredService<ISubscriptionRepository>();
var subscriptions = provider.GetRequiredService<ISubscriptionManager>();

// Add records through the repository used by the manager.
var eventType = typeof(OrderCreated).FullName!;
await repository.AddAsync(new Subscription(
    eventType,
    new Func<OrderCreated, CancellationToken, Task>(
        (order, cancellationToken) => Task.CompletedTask),
    handlerName: "BillingHandler",
    priority: 10));

IEnumerable<SubscriptionInfo> orderSubscriptions =
    await subscriptions.GetSubscriptionsAsync(eventType);

await subscriptions.DisableHandlerAsync("BillingHandler");

SubscriptionStatistics statistics = await subscriptions.GetStatisticsAsync();
Console.WriteLine(
    $"Active: {statistics.ActiveSubscriptions}; " +
    $"inactive: {statistics.InactiveSubscriptions}");

await subscriptions.EnableHandlerAsync("BillingHandler");

public sealed record OrderCreated(int OrderId);
```

## InMemoryRepositoryTests

The `InMemoryRepositoryTests` class provides comprehensive unit tests for the in-memory repository implementation. It validates basic CRUD operations, pagination, existence checks, counting, and clearing functionality for generic entity storage. These tests serve as both validation and usage examples for the `InMemoryRepository<T>` class.

Example usage:

```csharp
using DotnetEventBus.Repositories;
using DotnetEventBus.Models;

// Create an in-memory repository for a custom entity
var repository = new InMemoryRepository<TestEntity>();

// Add a new entity
var newEntity = new TestEntity { Id = "1", Name = "Sample Entity" };
var addedEntity = await repository.AddAsync(newEntity);

// Retrieve an entity by ID
var retrievedEntity = await repository.GetByIdAsync("1");
Console.WriteLine($"Retrieved: {retrievedEntity?.Name}");

// Update an existing entity
retrievedEntity.Name = "Updated Entity";
await repository.UpdateAsync(retrievedEntity);

// Check if entity exists
hasEntity = await repository.ExistsAsync("1");
Console.WriteLine($"Entity exists: {hasEntity}");

// Get total count of entities
var totalCount = await repository.CountAsync();
Console.WriteLine($"Total entities: {totalCount}");

// Get paginated results
var page = await repository.GetPagedAsync(1, 10);
Console.WriteLine($"Page 1 has {page.Items.Count} items, total {page.TotalCount} items");

// Clear all entities
await repository.ClearAsync();
var countAfterClear = await repository.CountAsync();
Console.WriteLine($"Entities after clear: {countAfterClear}");

// Access repository properties
Console.WriteLine($"Repository ID: {repository.Id}");
Console.WriteLine($"Repository Name: {repository.Name}");
```

## PipelineBuilder

`PipelineBuilder` (namespace `DotnetEventBus.Middleware`) composes event-processing middleware into a single `EventBusMiddleware` delegate using a chain-of-responsibility pattern. Middleware is registered with `Use` in the order it should run, and `Build` returns the composed pipeline. The same file defines the two types the pipeline works with: the `EventBusMiddleware` delegate and the `EventContext` class.

Public API:
- `PipelineBuilder Use(Func<EventBusMiddleware, EventBusMiddleware> middleware)` — registers a middleware factory. The factory receives the next step in the chain and returns the step it wraps. Throws `ArgumentNullException` when `middleware` is `null`. Returns the builder for chaining.
- `EventBusMiddleware Build()` — composes all registered middleware and returns the outermost step. The first registered middleware runs first.
- `void Clear()` — removes all registered middleware.
- `delegate Task EventBusMiddleware(EventContext context)` — a single pipeline step. A step either calls `next(context)` to continue the chain or returns without calling it to short-circuit.
- `EventContext` — the event data and metadata passed through the pipeline. Properties: `required string EventType`, `required object EventData`, `Dictionary<string, object> Metadata` (empty by default), `DateTime CreatedAt` (UTC, set on creation), `string? CorrelationId`, `bool IsProcessed`, `Exception? ProcessingException`.

Behavior notes:
- `Build()` takes a snapshot. Calling `Use` or `Clear` afterwards does not change a pipeline that has already been built.
- The innermost step is a no-op that completes immediately. `PipelineBuilder` does not invoke subscribers itself, so the chain only does what your middleware does.
- Factories are invoked each time the pipeline runs, not once at `Build()`. Keep them free of one-time setup. Put any shared state in fields or captured variables outside the factory.
- `PipelineBuilder` is not thread-safe for concurrent `Use` / `Clear` calls. Build the pipeline once at startup and reuse the returned delegate.

Example usage:
```csharp
using DotnetEventBus.Middleware;

var builder = new PipelineBuilder()
    .Use(next => async context =>
    {
        Console.WriteLine($"Before {context.EventType}");
        await next(context);
        Console.WriteLine($"After {context.EventType}");
    })
    .Use(next => context =>
    {
        context.Metadata["processedBy"] = "audit";
        return next(context);
    });

EventBusMiddleware pipeline = builder.Build();

var context = new EventContext
{
    EventType = "OrderCreated",
    EventData = new { OrderId = 42 },
    CorrelationId = Guid.NewGuid().ToString()
};

await pipeline(context);

// Remove all middleware and start over
builder.Clear();
```

## RateLimitingMiddleware

`RateLimitingMiddleware` (namespace `DotnetEventBus.Middleware`) is a sealed middleware component that limits how many events of each event type can pass through a pipeline within a sliding time window. It keeps a separate bucket for every `EventType` value, so a burst of one event type does not use up the quota of another. Create the middleware with a logger and its limits, then add its `Create` method to a `PipelineBuilder` with `Use`.

Public API:
- `RateLimitingMiddleware(ILogger<RateLimitingMiddleware> logger, int requestsPerWindow = 1000, TimeSpan? timeWindow = null)` — creates the middleware. Throws `ArgumentNullException` when `logger` is `null`. `requestsPerWindow` is the maximum number of events of one type accepted within the window (default `1000`). `timeWindow` is the length of the sliding window (default 60 seconds when `null`). The limits are not validated, so a `requestsPerWindow` of `0` or less rejects every event.
- `EventBusMiddleware Create(EventBusMiddleware next)` — returns the pipeline step. Before it calls `next`, it checks the bucket for `context.EventType`. If the quota is used up, it throws `RateLimitExceededException` and does not call `next`. Otherwise it records the event in the bucket and awaits `next(context)`.
- `public sealed class RateLimitExceededException : Exception` — thrown when an event type has exceeded its quota. Its constructor is `RateLimitExceededException(string message)`. The message has the form `Rate limit exceeded for event type: {EventType}`.

Behavior notes:
- The window is sliding. Each accepted event stores a timestamp, and timestamps older than `timeWindow` are dropped on the next check. Rejected events are not recorded, so they do not extend the block.
- Buckets are keyed by the `EventType` string using the default, case-sensitive comparison. A bucket is created the first time a type is seen and is never removed, so memory grows with the number of distinct event type names.
- The quota check and the recording are two separate lock acquisitions. Concurrent publishes of the same type can pass the check together, so the limit can be exceeded by up to the number of callers running at the same time.
- A rejected event is not passed to the rest of the pipeline. The returned task faults with `RateLimitExceededException`, so the caller of the pipeline must catch it.
- The middleware writes an `Information` log entry when each check starts and finishes, and a `Warning` entry when an event is rejected. Under high event volume, filter these entries in your logging configuration.

Example usage:
```csharp
using DotnetEventBus.Middleware;
using Microsoft.Extensions.Logging;

ILogger<RateLimitingMiddleware> logger = loggerFactory.CreateLogger<RateLimitingMiddleware>();

// Accept at most 100 events of each type every 10 seconds.
var rateLimiter = new RateLimitingMiddleware(logger, requestsPerWindow: 100, timeWindow: TimeSpan.FromSeconds(10));

EventBusMiddleware pipeline = new PipelineBuilder()
    .Use(rateLimiter.Create)
    .Build();

var context = new EventContext
{
    EventType = "OrderCreated",
    EventData = new { OrderId = 42 }
};

try
{
    await pipeline(context);
}
catch (RateLimitExceededException ex)
{
    // Log the rejection, retry later, or return HTTP 429 to the caller.
    Console.WriteLine(ex.Message);
}
```

## ErrorHandlingMiddleware

`ErrorHandlingMiddleware` (namespace `DotnetEventBus.Middleware`) is a sealed middleware component that runs the rest of the pipeline again when a step throws. After the last attempt fails, it can call an optional recovery callback. If the callback does not recover the event, the middleware throws `EventProcessingException` with the last exception as its `InnerException`. Create the middleware with a logger and its retry settings, then add its `Create` method to a `PipelineBuilder` with `Use`. Put the steps that should be retried after this middleware in the pipeline, because the retries re-run only the steps that come after it.

Public API:
- `ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger, int maxRetries = 3, TimeSpan? retryDelay = null, Func<EventContext, Exception, Task<bool>>? errorHandler = null)` — creates the middleware. Throws `ArgumentNullException` when `logger` is `null`. `maxRetries` is the total number of attempts, including the first one (default `3`). `retryDelay` is the fixed wait between attempts (default 1 second when `null`). `errorHandler` is the optional recovery callback. `maxRetries` and `retryDelay` are not validated by the constructor (see the notes below).
- `EventBusMiddleware Create(EventBusMiddleware next)` — returns the pipeline step that wraps `next`. The step calls `next(context)` once per attempt. The instance holds only read-only settings, so the same middleware can be used by several pipelines.
- `public sealed class EventProcessingException : Exception` — thrown when all attempts fail and the error handler does not recover the event. Its constructor is `EventProcessingException(string message, Exception? innerException = null)`. The message has the form `Event {EventType} failed after {maxRetries} retries`, and `InnerException` holds the exception from the last attempt.

Behavior for each event:
1. Calls `next(context)`. If it completes, the step sets `context.IsProcessed = true` and returns.
2. If it throws, the step stores the exception in `context.ProcessingException` and the attempt number in `context.Metadata["attempt"]` (as an `int`), logs a `Warning`, and waits `retryDelay` before the next attempt. The wait is not applied after the last attempt.
3. After the last attempt fails, if `errorHandler` is set, the step calls `errorHandler(context, lastException)`:
   - If it returns `true`, the event counts as recovered. The step sets `context.IsProcessed = true`, sets `context.Metadata["recoveredByHandler"] = true`, and returns normally.
   - If it returns `false`, the step continues with step 4.
4. Otherwise, the step sets `context.IsProcessed = false`, logs an `Error` with the last exception, and throws `EventProcessingException`.

Behavior notes:
- Every exception is retried, including `OperationCanceledException`. The delay does not observe any cancellation token, so cancellation does not stop the retries.
- The retry delay is fixed. There is no exponential backoff and no jitter. For those, use `RetryPolicy` (see [RetryPolicy](#retrypolicy)) inside a handler.
- `Metadata["attempt"]` holds the number of the most recent failed attempt. If the first attempt succeeds, the key is not set.
- An exception thrown by `errorHandler` is not caught. It propagates to the caller as is, and it is not retried.
- `maxRetries` is not validated by the constructor. With `0` or a negative value, the loop never runs: the step returns without calling `next`, sets `IsProcessed = false`, and throws nothing. Use a value of at least `1`. `AddErrorHandling` (below) rejects a negative value with `ArgumentOutOfRangeException`, but it does not reject `0`.
- Logging: an `Information` entry when `Create` is called, one at the start and one at the success of each event, a `Warning` for each failed attempt (with the exception message only), and an `Error` with the full exception when the event fails.

Example usage:
```csharp
using DotnetEventBus.Middleware;
using Microsoft.Extensions.Logging;

ILogger<ErrorHandlingMiddleware> logger = loggerFactory.CreateLogger<ErrorHandlingMiddleware>();

// Make up to 4 attempts, waiting 500 ms between them.
var errorHandling = new ErrorHandlingMiddleware(
    logger,
    maxRetries: 4,
    retryDelay: TimeSpan.FromMilliseconds(500),
    errorHandler: RecoverAsync);

async Task<bool> RecoverAsync(EventContext context, Exception exception)
{
    // Save the event for later processing, for example to a dead-letter store.
    await Task.Delay(10);

    // true: the event is treated as processed. false: EventProcessingException is thrown.
    return true;
}

EventBusMiddleware pipeline = new PipelineBuilder()
    .Use(errorHandling.Create)
    .Use(next => async context =>
    {
        // This step is retried when it throws, because it runs after the error handling step.
        await Task.Delay(10);
        await next(context);
    })
    .Build();

var context = new EventContext
{
    EventType = "OrderCreated",
    EventData = new { OrderId = 42 }
};

try
{
    await pipeline(context);
}
catch (EventProcessingException ex)
{
    // The event was not recovered. The exception from the last attempt is in InnerException.
    Console.WriteLine($"{ex.Message}: {ex.InnerException?.Message}");
}
```

The `AddErrorHandling` extension method on `PipelineBuilder` (namespace `DotnetEventBus.Configuration`) creates the middleware for you and registers it. It takes an `ILoggerFactory` instead of a logger, and it accepts the same `maxRetries`, `retryDelay`, and `errorHandler` parameters:

```csharp
using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;

EventBusMiddleware pipeline = new PipelineBuilder()
    .AddErrorHandling(loggerFactory, maxRetries: 3, retryDelay: TimeSpan.FromSeconds(2))
    .Build();
```

## PipelineBuilderTests

The `PipelineBuilderTests` class provides comprehensive unit tests for the `PipelineBuilder` middleware pipeline construction. It verifies middleware registration, execution order, context manipulation, error handling, and pipeline building scenarios. The tests cover both synchronous and asynchronous middleware execution, short-circuiting behavior, exception handling, and proper initialization of event context.

## EventBusIntegrationTests

The `EventBusIntegrationTests` class provides comprehensive integration tests for the event bus system, validating end-to-end scenarios including event publishing, subscription, handler execution, error handling, retry policies, circuit breakers, metrics collection, and middleware pipelines. These tests ensure the event bus operates correctly under various conditions including parallel publishing, batch processing, priority-based execution, and failure scenarios.

Example usage:
```csharp
using DotnetEventBus.Middleware;
using Xunit;

// Create a pipeline builder
var builder = new PipelineBuilder();

// Add middleware components to the pipeline
builder.Use(next => async context => {
    // Pre-processing logic
    context.Metadata["startedAt"] = DateTime.UtcNow;
    await next(context);
    // Post-processing logic
});

builder.Use(next => async context => {
    // Validation middleware
    if (context.EventData == null)
        throw new InvalidOperationException("Event data cannot be null");
    await next(context);
});

builder.Use(next => async context => {
    // Processing middleware
    context.IsProcessed = true;
    await next(context);
});

// Build the pipeline
var pipeline = builder.Build();

// Create and execute the pipeline with an event context
var context = new EventContext {
    EventType = "OrderPlaced",
    EventData = new { OrderId = 123, Amount = 99.99 }
};

await pipeline(context);

// Verify the context was processed
Assert.True(context.IsProcessed);
Assert.ContainsKey(context.Metadata, "startedAt");
```

## TestEvent

The `TestEvent` class is a simple test event used throughout the event bus unit tests. It contains basic properties for testing event publishing, subscription, and handler invocation scenarios. This event is commonly used to validate core event bus functionality including handler execution, priority-based invocation, and subscription management.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Tests;
using Microsoft.Extensions.DependencyInjection;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddEventBus();
var provider = services.BuildServiceProvider();
var eventBus = provider.GetRequiredService<IEventBus>();

// Define a test event with sample data
var testEvent = new TestEvent {
    Data = "Sample event data",
    Value = 42
};

// Subscribe an asynchronous handler
eventBus.Subscribe<TestEvent>(
    async (@event, ct) => {
        Console.WriteLine($"Received event with Data: {@event.Data}, Value: {@event.Value}");
        await Task.CompletedTask;
    },
    handlerName: "TestHandler"
);

// Subscribe a synchronous handler
eventBus.SubscribeSync<TestEvent>(
    @event => {
        Console.WriteLine($"Sync handler received: {@event.Data}");
    },
    handlerName: "SyncTestHandler"
);

// Subscribe with priority
eventBus.Subscribe<TestEvent>(
    async (@event, ct) => {
        Console.WriteLine("High priority handler");
        await Task.CompletedTask;
    },
    handlerName: "HighPriorityHandler",
    priority: (int)HandlerPriority.High
);

// Publish the test event
var result = await eventBus.PublishAsync(testEvent);

// Verify the event was processed
Console.WriteLine($"Handlers invoked: {result.HandlersInvoked}");
```

## TestFilterEvent

The `TestFilterEvent` class is a test event used for filtering scenarios within the event bus system. It contains properties that can be filtered on including order ID, amount, status, and region. This event is primarily used in unit tests for the `EventFilter<T>` class to validate filtering logic.

Example usage:

```csharp
using DotnetEventBus.Advanced;

// Define a test event
var testEvent = new TestFilterEvent {
    OrderId = 123,
    Amount = 99.99m,
    Status = "Pending",
    Region = "US"
};

// Create a filter to match events with OrderId > 100
var filter = new EventFilter<TestFilterEvent>()
    .Where(e => e.OrderId > 100);

// Check if the event matches the filter
bool matches = filter.Matches(testEvent); // Returns true
```

## NewCoreFunctionalityTests

The `NewCoreFunctionalityTests` class provides comprehensive integration tests for new core functionality of the DotnetEventBus library. It validates priority-based handler invocation, middleware pipeline execution, retry mechanisms, dead letter queue functionality, and subscription management. These tests ensure the event bus operates correctly under various conditions including parallel publishing, priority-based execution, and failure scenarios.

Example usage:

```csharp
using DotnetEventBus.Configuration;
using DotnetEventBus.Middleware;
using DotnetEventBus.Models;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddLogging();
var serviceProvider = services.BuildServiceProvider();

// Create event bus with default options
var options = new EventBusOptions();
var bus = new EventBus(options: options, serviceProvider: serviceProvider);

// Subscribe handlers with different priorities
bus.Subscribe<string>((e, ct) => {
    Console.WriteLine("Low priority handler executed");
    return Task.CompletedTask;
}, "LowPriority", priority: 0);

bus.Subscribe<string>((e, ct) => {
    Console.WriteLine("High priority handler executed");
    return Task.CompletedTask;
}, "HighPriority", priority: 10);

// Publish an event - handlers will be invoked in priority order
await bus.PublishAsync("test-event");

// Subscribe with retry policy
var retryOptions = new EventBusOptions { MaxRetryAttempts = 2 };
var retryBus = new EventBus(options: retryOptions, serviceProvider: serviceProvider);
int attempts = 0;

retryBus.Subscribe<string>((e, ct) => {
    attempts++;
    if (attempts < 3) throw new Exception("Temporary failure");
    return Task.CompletedTask;
});

await retryBus.PublishAsync("retry-test");
Console.WriteLine($"Handler attempted {attempts} times"); // Will be 3

// Unsubscribe to prevent future invocations
var subscription = bus.SubscribeSync<string>(e => Console.WriteLine("Handler called"));
await bus.PublishAsync("first"); // Handler called
subscription.Dispose();
await bus.PublishAsync("second"); // Handler not called
```

Example usage:

```csharp
using DotnetEventBus.Advanced;

// Define a test event
var testEvent = new TestFilterEvent {
    OrderId = 123,
    Amount = 99.99m,
    Status = "Pending",
    Region = "US"
};

// Create a filter to match events with OrderId > 100
var filter = new EventFilter<TestFilterEvent>()
    .Where(e => e.OrderId > 100);

// Check if the event matches the filter
bool matches = filter.Matches(testEvent); // Returns true

// Create a filter with multiple predicates
var complexFilter = new EventFilter<TestFilterEvent>()
    .Where(e => e.OrderId > 100)
    .WhereProperty(e => e.Status, "Pending")
    .WherePropertyInRange(e => e.Amount, 50, 500);

// Check if the event matches all predicates
bool complexMatches = complexFilter.Matches(testEvent); // Returns true

// Create a filter that excludes completed events
var notFilter = new EventFilter<TestFilterEvent>()
    .Not(e => e.Status == "Completed");

// Check if the event matches the inverted predicate
bool notMatches = notFilter.Matches(testEvent); // Returns true

// Filter events by region (null-safe)
var regionFilter = new EventFilter<TestFilterEvent>()
    .WhereProperty(e => e.Region, "US");

var usEvent = new TestFilterEvent { Region = "US" };
bool regionMatches = regionFilter.Matches(usEvent); // Returns true

var nullRegionEvent = new TestFilterEvent { Region = null };
bool nullRegionMatches = regionFilter.Matches(nullRegionEvent); // Returns false

// Filter events by string contains (case-insensitive)
var statusFilter = new EventFilter<TestFilterEvent>()
    .WherePropertyContains(e => e.Status, "end");

var endStatusEvent = new TestFilterEvent { Status = "Pending" };
bool containsMatches = statusFilter.Matches(endStatusEvent); // Returns true
```

## BatchEventPublisher

`BatchEventPublisher` (namespace `DotnetEventBus.Services`) collects `EventEnvelope` instances in an in-memory buffer and passes them to your handler as an `EventBatch`. The buffer is flushed when it reaches `batchSize`, when `flushInterval` has passed when an event is added, or when you call `FlushAsync()`. Events are held in memory only; nothing is persisted.

### Constructor

```csharp
public BatchEventPublisher(
    ILogger<BatchEventPublisher> logger,
    int batchSize = 100,
    TimeSpan? flushInterval = null)
```

- **logger**: Required; `null` throws `ArgumentNullException`.
- **batchSize**: Number of buffered events that triggers a flush. Must be greater than `0`; otherwise `ArgumentException` is thrown.
- **flushInterval**: Maximum time between flushes. When `null`, it defaults to 10 seconds.

### Public Members

| Member | Description |
|--------|-------------|
| `SetFlushHandler(Func<EventBatch, Task> handler)` | Registers a handler that receives each flushed batch as a whole. `null` throws `ArgumentNullException`. If the handler throws, the exception is logged and rethrown to the caller of `AddEventAsync`, `AddEventsAsync`, or `FlushAsync`. The failed batch is not put back in the buffer. |
| `SetFlushHandlerWithResult(Func<EventEnvelope, Task<EventBatchItemResult>> handler, Action<BatchPublishResult>? onBatchComplete = null)` | Registers a handler that is called once per event, with error isolation. A throwing handler is recorded as a failed item and the rest of the batch continues. `onBatchComplete` receives the aggregated `BatchPublishResult` after each flush. `null` for `handler` throws `ArgumentNullException`. |
| `Task<bool> AddEventAsync(EventEnvelope envelope)` | Adds one event to the buffer and flushes if a trigger is met. Returns `true` when the event was buffered, or `false` when the envelope is invalid (`EventType` is empty or `Payload` is `null`). `null` throws `ArgumentNullException`. |
| `Task AddEventsAsync(IEnumerable<EventEnvelope> envelopes)` | Calls `AddEventAsync` for each item in order. A `null` item throws `ArgumentException`, but the items before it have already been added. |
| `Task FlushAsync()` | Flushes all buffered events now. Does nothing when the buffer is empty. |
| `int GetBufferSize()` | Returns the number of events currently buffered. |
| `BatchPublisherStats GetStats()` | Returns `BufferedEventCount`, `BufferedEventSize`, and `LastFlushTime`. `BufferedEventSize` currently equals the event count, not a byte size. |
| `string ToString()` | Returns a summary of the stats. |

### Flush Behavior

- **Triggers are checked only inside `AddEventAsync`.** The class has no timer. An event sitting in an idle buffer is not flushed when `flushInterval` passes. It waits until the next add or until you call `FlushAsync()`. Call `FlushAsync()` on shutdown so buffered events are not lost.
- **The interval is measured from the last flush**, or from construction if nothing has flushed yet. If the publisher has been idle longer than `flushInterval`, the next added event is flushed immediately as a batch of one.
- **Without a handler, events are dropped.** The buffer is cleared before the handler is looked up, so a flush with no handler logs a warning and discards the events. Register a handler before adding events.
- **A per-event handler takes precedence.** If both `SetFlushHandler` and `SetFlushHandlerWithResult` were called, only the per-event handler runs.
- **Handlers run outside the internal lock.** Concurrent `AddEventAsync` calls can flush concurrently, so batches may be processed in parallel and out of addition order.
- **Failed events are not moved to a dead letter queue.** Inspect `BatchPublishResult.FailedEvents` and handle them yourself.

### EventBatchItemResult

| Property | Type | Description |
|----------|------|-------------|
| `EventId` | `string?` | ID of the event this result describes. |
| `EventType` | `string?` | Type of the event. |
| `Success` | `bool` | Defaults to `false`. Your handler must set it to `true` on success. |
| `ErrorMessage` | `string?` | Error text for a failed item. |
| `Exception` | `Exception?` | Exception for a failed item. Set automatically when the handler throws. |

### BatchPublishResult

| Property | Type | Description |
|----------|------|-------------|
| `BatchId` | `string?` | ID of the flushed `EventBatch`. |
| `SucceededCount` | `int` | Number of items whose `Success` is `true`. |
| `FailedCount` | `int` | Number of items whose `Success` is `false`, including thrown exceptions. |
| `AllSucceeded` | `bool` | `true` when `FailedCount` is `0`. |
| `EventResults` | `List<EventBatchItemResult>` | One entry per event in the batch, in order. |
| `FailedEvents` | `IEnumerable<EventBatchItemResult>` | The entries from `EventResults` where `Success` is `false`. |

### BatchPublisherStats

| Property | Type | Description |
|----------|------|-------------|
| `BufferedEventCount` | `int` | Number of events waiting in the buffer. |
| `BufferedEventSize` | `int` | Currently the same value as `BufferedEventCount`. |
| `LastFlushTime` | `DateTime` | UTC time of the most recent flush, or construction time if none has happened. |

### Validation Helpers

`BatchEventPublisherValidation` adds extension methods to `BatchEventPublisher`:

- `Validate()` returns an `IReadOnlyList<string>` of problems, empty when valid. Passing `null` throws `ArgumentNullException`.
- `IsValid()` returns `true` when `Validate()` finds no problems.
- `EnsureValid()` throws `ArgumentException` listing the problems.

### Example Usage

Batch-level handler with a final flush on shutdown:

```csharp
using DotnetEventBus.Models;
using DotnetEventBus.Services;
using Microsoft.Extensions.Logging;

var publisher = new BatchEventPublisher(
    loggerFactory.CreateLogger<BatchEventPublisher>(),
    batchSize: 50,
    flushInterval: TimeSpan.FromSeconds(5));

publisher.SetFlushHandler(batch =>
{
    Console.WriteLine($"Batch {batch.BatchId}: {batch.Events.Count} events");
    return Task.CompletedTask;
});

// Invalid envelopes (empty EventType or null Payload) return false and are not buffered.
bool added = await publisher.AddEventAsync(new EventEnvelope
{
    EventType = "OrderPlaced",
    Payload = "{ \"orderId\": 42 }"
});

// On shutdown, flush whatever is still buffered.
await publisher.FlushAsync();
```

Per-event handler with error isolation and a batch summary:

```csharp
var publisher = new BatchEventPublisher(logger, batchSize: 100);

publisher.SetFlushHandlerWithResult(
    async envelope =>
    {
        try
        {
            await SendToBrokerAsync(envelope); // your delivery code
            return new EventBatchItemResult
            {
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new EventBatchItemResult
            {
                EventId = envelope.EventId,
                EventType = envelope.EventType,
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    },
    onBatchComplete: result =>
    {
        Console.WriteLine($"Batch {result.BatchId}: {result.SucceededCount} ok, {result.FailedCount} failed");

        foreach (var failed in result.FailedEvents)
        {
            // Retry later or write to your own dead letter store.
            Console.WriteLine($"Failed {failed.EventId} ({failed.EventType}): {failed.ErrorMessage}");
        }
    });

await publisher.AddEventsAsync(envelopes);
await publisher.FlushAsync();
```

## BatchEventPublisherTests

The `BatchEventPublisherTests` class provides comprehensive unit tests for the `BatchEventPublisher` batch event publishing functionality. It validates event addition to batches, batch flushing when the batch size is reached, error handling for invalid events, and constructor validation for proper argument handling. These tests serve as both validation and usage examples for working with the batch event publishing system.

Example usage:

```csharp
using DotnetEventBus.Services;
using DotnetEventBus.Models;
using Microsoft.Extensions.Logging;
using Xunit;

// Create a batch event publisher with a batch size of 10 events
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var publisher = new BatchEventPublisher(loggerFactory.CreateLogger<BatchEventPublisher>(), batchSize: 10);

// Add valid events to the batch
var envelope1 = new EventEnvelope { EventType = "OrderCreated", Payload = "{ \"orderId\": 123 }" };
var envelope2 = new EventEnvelope { EventType = "PaymentProcessed", Payload = "{ \"paymentId\": 456 }" };

var added1 = await publisher.AddEventAsync(envelope1); // Returns true
var added2 = await publisher.AddEventAsync(envelope2); // Returns true

// Set a flush handler to process batches when they're full
var flushedBatches = new List<EventBatch>();
publisher.SetFlushHandler(async batch => {
    flushedBatches.Add(batch);
    await Task.CompletedTask;
});

// Add enough events to trigger a flush (10 events with batchSize=10)
for (int i = 0; i < 10; i++) {
    var envelope = new EventEnvelope { EventType = "Event" + i, Payload = "payload" };
    await publisher.AddEventAsync(envelope);
}

// Verify the batch was flushed
Assert.Single(flushedBatches);
Assert.Equal(10, flushedBatches[0].Events.Count);

// Flush remaining events manually
await publisher.FlushAsync();
```

## EventMessageModelTests

The `EventMessageModelTests` class provides comprehensive unit tests for the `EventMessage` model behavior, validating message retry functionality, header management, and processing state management. These tests ensure proper handling of event message operations including creating retry messages, managing custom headers, and tracking processing attempts.

Example usage:

```csharp
using DotnetEventBus.Models;

// Create a new event message
var message = new EventMessage("OrderService.OrderPlaced", "{\"orderId\": 123, \"amount\": 99.99}");

// Set message metadata
message.CorrelationId = "corr-12345";
message.Source = "order-service";
message.ProcessingAttempts = 0;

// Add custom headers for tracing and routing
message.AddHeader("x-trace-id", "trace-abc-123");
message.AddHeader("x-region", "eu-west-1");
message.AddHeader("x-priority", "high");

// Retrieve a header value
string traceId = message.GetHeader("x-trace-id"); // Returns "trace-abc-123"
string unknownHeader = message.GetHeader("x-unknown"); // Returns null

// Create a retry message (increments ProcessingAttempts and generates new MessageId)
var retryMessage = message.CreateRetry();
Console.WriteLine($"Retry has {retryMessage.ProcessingAttempts} attempts"); // 1
Console.WriteLine($"Original MessageId: {message.MessageId}, Retry MessageId: {retryMessage.MessageId}"); // Different IDs

// Verify headers are preserved in retry
string retryTraceId = retryMessage.GetHeader("x-trace-id"); // Returns "trace-abc-123"
```

## RetryPolicyTests

The `RetryPolicyTests` class provides comprehensive unit tests for the retry policy functionality within the DotnetEventBus library. It validates various retry scenarios including successful operations without retries, transient failures with automatic retries, maximum retry limits, exponential backoff with jitter, delay capping, exception filtering, and configuration validation for retry parameters.

Example usage:

```csharp
using DotnetEventBus.Retry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Configure retry policy with exponential backoff
var retryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    MaxDelay = TimeSpan.FromSeconds(5),
    BackoffMultiplier = 2.0,
    EnableJitter = true
};

// Execute an operation with retry policy
int attemptCount = 0;
var result = await retryPolicy.ExecuteAsync(async () =>
{
    attemptCount++;
    if (attemptCount < 3)
    {
        throw new InvalidOperationException("Temporary failure");
    }
    return "Success";
});

Console.WriteLine($"Operation succeeded after {attemptCount} attempts");

// Configure retry policy with immediate retries
var immediateRetryPolicy = new ImmediateRetryPolicy
{
    MaxAttempts = 5
};

// Execute an operation that eventually succeeds
var successResult = await immediateRetryPolicy.ExecuteAsync(async () =>
{
    if (DateTime.UtcNow.Second % 2 == 0)
    {
        return "Success";
    }
    throw new InvalidOperationException("Temporary failure");
});

// Configure retry policy with custom exception filter
var filteredRetryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 3,
    InitialDelay = TimeSpan.FromMilliseconds(50),
    ExceptionFilter = ex => ex is InvalidOperationException || ex is ArgumentException
};

// Execute an operation that throws a retryable exception
var filteredResult = await filteredRetryPolicy.ExecuteAsync(async () =>
{
    throw new InvalidOperationException("This will be retried");
});

// Configure retry policy with capped delay
var cappedRetryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 5,
    InitialDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromSeconds(2), // Maximum delay will be capped at 2 seconds
    BackoffMultiplier = 2.0
};

// Execute an operation with capped delay between retries
var cappedResult = await cappedRetryPolicy.ExecuteAsync(async () =>
{
    throw new InvalidOperationException("Will retry with capped delay");
});

// Configure retry policy with jitter disabled for consistent delays
var consistentRetryPolicy = new ExponentialBackoffRetryPolicy
{
    MaxAttempts = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    EnableJitter = false // Delays will be exactly InitialDelay * multiplier
};

// Execute an operation with consistent delays
var consistentResult = await consistentRetryPolicy.ExecuteAsync(async () =>
{
    throw new InvalidOperationException("Will retry with consistent delays");
});
```

## DeadLetterQueueHandlingExample

The `DeadLetterQueueHandlingExample` class demonstrates error handling, retries, and recovery mechanisms in the DotnetEventBus library. It shows how to configure the event bus to handle failed events, manage dead letter queues, and reprocess failed events.

## PerformanceMetricsMonitoringExample


The `PerformanceMetricsMonitoringExample` class demonstrates system metrics collection, handler performance profiling, and real-time monitoring capabilities. It shows how to track event publishing metrics, measure handler execution times, and generate performance reports for performance optimization and monitoring purposes.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Examples;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection with event bus and enable metrics
var services = new ServiceCollection();
services.AddEventBus(options =>
{
    options.AllowParallelHandling = false;
    options.EnableMetrics = true;
    options.EnableDetailedLogging = true;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
var metricsCollector = serviceProvider.GetRequiredService<IMetricsCollector>();
var performanceProfiler = serviceProvider.GetRequiredService<IPerformanceProfiler>();

// Define event with metrics properties
var dataProcessingEvent = new PerformanceMetricsMonitoringExample.DataProcessingEvent
{
    ProcessId = "PROC-001",
    DataSize = 1500,
    ProcessType = "Medium"
};

// Subscribe handlers
var fastProcessor = new PerformanceMetricsMonitoringExample.FastProcessorHandler();
var mediumProcessor = new PerformanceMetricsMonitoringExample.MediumProcessorHandler();
var slowProcessor = new PerformanceMetricsMonitoringExample.SlowProcessorHandler();

// Publish event and collect metrics
await eventBus.PublishAsync(dataProcessingEvent);

// Get system metrics
var systemMetrics = metricsCollector.GetSystemMetrics();
Console.WriteLine($"Total Events Published: {systemMetrics.TotalEventsPublished}");
Console.WriteLine($"Success Rate: {systemMetrics.SuccessRate:P2}");
Console.WriteLine($"Average Latency: {systemMetrics.AverageLatency:F2}ms");

// Get handler-specific metrics
var handlerMetrics = metricsCollector.GetHandlerMetrics("FastProcessorHandler");
if (handlerMetrics is not null)
{
    Console.WriteLine($"Handler Execution Count: {handlerMetrics.ExecutionCount}");
    Console.WriteLine($"Average Duration: {handlerMetrics.AverageDuration:F2}ms");
}

// Generate performance profile report
var profileReport = performanceProfiler.GenerateReport();
Console.WriteLine(profileReport);
```

## BatchPublishingOptimizationExample

The `BatchPublishingOptimizationExample` class demonstrates efficient event publishing using batch operations to improve throughput and resource utilization. It compares individual event publishing with batch publishing, showing performance improvements and memory efficiency benefits.

## BatchPublishingOptimizationExampleJsonExtensions

The `BatchPublishingOptimizationExampleJsonExtensions` class provides System.Text.Json serialization and deserialization extensions for the event types in the `BatchPublishingOptimizationExample` class (`LogEntryEvent` and `AnalyticsEvent`). It enables easy conversion between event instances and JSON strings with support for both compact and indented formatting, making it ideal for event serialization, storage, and API communication.

Example usage:

```csharp
using DotnetEventBus.Examples;
using System;

// Create a log entry event
var logEvent = new BatchPublishingOptimizationExample.LogEntryEvent
{
    LogId = "LOG-001",
    Level = "Info",
    Message = "Application started successfully",
    Timestamp = DateTime.UtcNow,
    Source = "Application.Startup",
    Category = "System"
};

// Serialize to compact JSON
string compactJson = logEvent.ToJson();
Console.WriteLine(compactJson);

// Serialize to pretty-printed JSON
string prettyJson = logEvent.ToJson(indented: true);
Console.WriteLine(prettyJson);

// Deserialize from JSON
var deserializedLogEvent = BatchPublishingOptimizationExampleJsonExtensions.FromJson(compactJson);
Console.WriteLine($"Deserialized log event: {deserializedLogEvent?.Message}");

// Try to deserialize with error handling
if (BatchPublishingOptimizationExampleJsonExtensions.TryFromJson(compactJson, out var safeLogEvent))
{
    Console.WriteLine("Successfully deserialized log event");
}

// Create an analytics event
var analyticsEvent = new BatchPublishingOptimizationExample.AnalyticsEvent
{
    EventType = "PageView",
    UserId = "USER-123",
    SessionId = "SESSION-456",
    Timestamp = DateTime.UtcNow,
    Properties = new System.Collections.Generic.Dictionary<string, object>
    {
        { "page", "/products/premium" },
        { "referrer", "/home" },
        { "durationMs", 2500 },
        { "deviceType", "Desktop" },
        { "browser", "Chrome" }
    }
};

// Serialize analytics event to JSON
string analyticsJson = analyticsEvent.ToJson();
Console.WriteLine(analyticsJson);

// Deserialize analytics event from JSON
var deserializedAnalyticsEvent = BatchPublishingOptimizationExampleJsonExtensions.FromJson(analyticsJson);
Console.WriteLine($"Deserialized analytics event type: {deserializedAnalyticsEvent?.EventType}");

// Try to deserialize analytics event with error handling
if (BatchPublishingOptimizationExampleJsonExtensions.TryFromJson(analyticsJson, out var safeAnalyticsEvent))
{
    Console.WriteLine("Successfully deserialized analytics event");
}
```

## InMemoryEventCache

`InMemoryEventCache` (namespace `DotnetEventBus.Caching`) implements `IEventCache` and provides a thread-safe, in-memory caching mechanism with LRU (Least Recently Used) eviction policy and automatic expiration cleanup. It uses a concurrent dictionary for storage and maintains hit/miss/eviction statistics.

Public API:

- `InMemoryEventCache(int maxCapacity = 10000)` creates a cache instance with the specified maximum capacity (default 10,000 items).
- `int Capacity { get; }` gets the maximum number of items the cache can hold before eviction occurs.
- `Task<T?> GetAsync<T>(string key)` where T : class retrieves an item by key, returning null if not found or expired. Updates the item's last accessed time for LRU tracking on hit.
- `Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)` where T : class stores an item with optional expiration time. Automatically evicts the least recently used item if at capacity.
- `Task RemoveAsync(string key)` removes an item by key.
- `Task<bool> ExistsAsync(string key)` checks if an item exists and is not expired.
- `Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys)` where T : class retrieves multiple items by their keys.
- `Task RemoveManyAsync(IEnumerable<string> keys)` removes multiple items by their keys.
- `Task ClearAsync()` removes all items from the cache.
- `Task<CacheStats> GetStatsAsync()` returns cache statistics including hits, misses, evictions, total items, and estimated memory usage.
- `void Dispose()` stops the background cleanup task and releases resources.

The cache automatically removes expired entries via a background task that runs every minute. When the cache reaches its capacity limit, it evicts the least recently accessed item before adding a new one.

Example usage:

```csharp
using DotnetEventBus.Caching;
using Microsoft.Extensions.DependencyInjection;

// Register the cache with DI (optional)
var services = new ServiceCollection();
services.AddSingleton<InMemoryEventCache>(sp => new InMemoryEventCache(capacity: 5000));

var serviceProvider = services.BuildServiceProvider();
var cache = serviceProvider.GetRequiredService<InMemoryEventCache>();

// Set a value with default expiration (no expiration)
await cache.SetAsync("user:123", new { Id = 123, Name = "John Doe" });

// Set a value with explicit expiration
await cache.SetAsync("temp:token", "abc123", TimeSpan.FromMinutes(5));

// Retrieve a value
var user = await cache.GetAsync<dynamic>("user:123");
Console.WriteLine($"User: {user?.Name}");

// Check if key exists
bool exists = await cache.ExistsAsync("user:123");
Console.WriteLine($"Exists: {exists}");

// Get cache statistics
var stats = await cache.GetStatsAsync();
Console.WriteLine($"Hits: {stats.Hits}, Misses: {stats.Misses}, Evictions: {stats.Evictions}");

// Remove a value
await cache.RemoveAsync("user:123");

// Clear all values
await cache.ClearAsync();

// Dispose when finished (stops background cleanup)
cache.Dispose();
```

## InMemoryEventCacheState

The `InMemoryEventCacheState` class represents the serialized state of an `InMemoryEventCache` instance. It captures cache statistics including total items, hits, misses, and configuration details like maximum capacity. This type is primarily used with the `InMemoryEventCacheJsonExtensions` class for JSON serialization and deserialization of cache state.

Example usage:

```csharp
using DotnetEventBus.Caching;
using System;

// Create a cache instance
var cache = new InMemoryEventCache();

// Add some events to the cache
await cache.AddAsync("event1", new object());
await cache.AddAsync("event2", new object());

// Get cache statistics
var stats = await cache.GetStatsAsync();

// Serialize cache state to JSON
string cacheStateJson = cache.ToJson();
Console.WriteLine(cacheStateJson);

// Serialize with pretty printing
string prettyJson = cache.ToJson(indented: true);
Console.WriteLine(prettyJson);

// Deserialize from JSON (returns new cache instance)
var deserializedCache = InMemoryEventCacheJsonExtensions.FromJson(cacheStateJson);
Console.WriteLine("Deserialized cache created successfully");

// Try to deserialize with error handling
if (InMemoryEventCacheJsonExtensions.TryFromJson(cacheStateJson, out var safeCache))
{
    Console.WriteLine("Successfully deserialized cache state");
}

// Access state properties directly
var state = new InMemoryEventCacheState
{
    Stats = new CacheStatistics
    {
        TotalItems = 1000,
        Hits = 950,
        Misses = 50
    },
    MaxCapacity = 10000
};

string stateJson = System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions
{
    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    WriteIndented = true
});
Console.WriteLine(stateJson);
```

## HttpEventPublisher

The `HttpEventPublisher` class POSTs an event as JSON to a remote HTTP endpoint. It retries transient failures with exponential backoff, applies a per-attempt timeout, and adds a correlation ID header to each request. It never throws for delivery failures; instead it returns an `HttpPublishResult`. Namespace: `DotnetEventBus.Integration`.

### Constructor

```csharp
public HttpEventPublisher(
    HttpClient httpClient,
    ILogger<HttpEventPublisher> logger,
    HttpEventPublisherOptions? options = null)
```

- **httpClient**: Client used for every request. Required; `null` throws `ArgumentNullException`.
- **logger**: Required; `null` throws `ArgumentNullException`.
- **options**: Retry, timeout, and correlation settings. When `null`, default options are used.

### Public Members

| Member | Description |
|--------|-------------|
| `PublishAsync(string url, object eventData, Dictionary<string, string>? customHeaders = null, string? contentType = null)` | Serializes `eventData` to JSON and POSTs it to `url`, with retries. Returns an `HttpPublishResult`. `url` and `eventData` must not be `null`; otherwise `ArgumentNullException` is thrown. |

### HttpEventPublisherOptions

| Property | Default | Description |
|----------|---------|-------------|
| `MaxRetries` | `3` | Maximum number of attempts in total, including the first one. Despite the name, the value is not a count of retries after the first attempt. Values of `0` or less make no request and return a failed result. |
| `RetryDelayMs` | `1000` | Base delay in milliseconds. The delay before attempt `n + 1` is `RetryDelayMs * 2^(n-1)`, so the defaults wait 1 s, then 2 s. No delay follows the last attempt. |
| `Timeout` | 30 seconds | Timeout applied to each attempt separately. |
| `CorrelationIdHeaderName` | `"X-Correlation-Id"` | Header that receives a new GUID on each attempt. Set to `null` or an empty string to disable it. |

### HttpPublishResult

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | `true` when the response has a 2xx status code. |
| `StatusCode` | `int` | HTTP status code of the last response. `0` when no response was received (network error, timeout, or exhausted attempts without a response). |
| `ErrorMessage` | `string?` | Response body for a non-success response, or the exception message for a transport failure. `null` on success. |

`HttpPublishResult` also has a public constructor `(bool success, int statusCode, string? errorMessage)`, mainly for tests and custom wrappers.

### Delivery Behavior

- **Request**: `POST` with the JSON-serialized `eventData`. The default `Content-Type` is `application/json` (UTF-8). Pass `contentType` to override it.
- **Custom headers**: Entries in `customHeaders` are added to the content headers of each request. Header names and values are validated by `HttpHeaders`; an invalid entry is treated as a failed attempt rather than thrown to the caller.
- **Retries**: Retried on HTTP 5xx, HTTP 408, network errors, and per-attempt timeouts. Other non-2xx responses (for example 4xx, or 3xx) return immediately without retrying.
- **Serialization errors**: A value that `System.Text.Json` cannot serialize is also caught and retried like any other exception. After the final attempt the call returns a failed result with `StatusCode = 0`.
- **Cancellation**: `PublishAsync` has no `CancellationToken` parameter. Stop a publish only by its `Timeout`.
- **Correlation ID**: The correlation header is added to `httpClient.DefaultRequestHeaders`, not to the individual request. Each attempt appends a value, so on a long-lived or shared `HttpClient` the header accumulates values across calls. Use a dedicated `HttpClient` for this publisher, or set `CorrelationIdHeaderName` to `null` if you do not need the header.
- **Logging**: Successful publishes are logged at Information. Each failed attempt is logged at Warning, including the exception for transport failures.

### Example Usage

```csharp
using DotnetEventBus.Integration;
using Microsoft.Extensions.Logging;

var options = new HttpEventPublisherOptions
{
    MaxRetries = 4,
    RetryDelayMs = 500,
    Timeout = TimeSpan.FromSeconds(10)
};

// Use a dedicated HttpClient so the correlation header does not accumulate
// across unrelated publishers.
var publisher = new HttpEventPublisher(
    new HttpClient(),
    loggerFactory.CreateLogger<HttpEventPublisher>(),
    options);

var headers = new Dictionary<string, string>
{
    ["Authorization"] = "Bearer <token>",
    ["X-Source"] = "order-service"
};

var result = await publisher.PublishAsync(
    "https://inventory.example.com/events/order-created",
    new { OrderId = "ORD-2026-001", Amount = 299.99m },
    customHeaders: headers);

if (result.Success)
{
    Console.WriteLine($"Published, status {result.StatusCode}");
}
else
{
    Console.WriteLine($"Publish failed (status {result.StatusCode}): {result.ErrorMessage}");
}
```

`PublishAsync` always JSON-serializes `eventData`, so `contentType` should only be changed when the receiver expects a different media type for a JSON body (for example `application/cloudevents+json`).

## WebhookHandler

The `WebhookHandler` class manages webhook subscriptions and delivers events to external HTTP endpoints. It handles subscription bookkeeping (subscribe, unsubscribe, update, list), HMAC-SHA256 signature generation and verification, and JSON delivery with a configurable retry policy. Namespace: `DotnetEventBus.Integration`.

### Constructor

```csharp
public WebhookHandler(string? signingSecret = null, ILogger<WebhookHandler>? logger = null)
```

- **signingSecret**: Secret used by `GenerateSignature` and `VerifySignature`. When null or empty, signatures are empty strings and verification is skipped.
- **logger**: Optional logger for subscription and delivery events.

### Public Members

| Member | Description |
|--------|-------------|
| `Subscribe(WebhookSubscription)` | Registers a subscription. Assigns a new GUID to `Id` if it is null. Throws `ArgumentNullException` for null. |
| `Unsubscribe(string subscriptionId)` | Removes a subscription. Returns `false` if no subscription has that ID. |
| `GetWebhooksForEvent(string eventType)` | Returns active subscriptions whose `EventTypes` contain `eventType` or `"*"`. The result is lazily evaluated. |
| `GetAllSubscriptions()` | Returns a snapshot list of all subscriptions, active or not. |
| `UpdateSubscription(string subscriptionId, Action<WebhookSubscription> updates)` | Applies `updates` to the matching subscription. Returns `false` if no subscription has that ID. |
| `GenerateSignature(string payload)` | Returns the Base64-encoded HMAC-SHA256 of the UTF-8 payload using the signing secret, or `string.Empty` if no secret is set. |
| `VerifySignature(string payload, string providedSignature)` | Compares the expected signature with `providedSignature`. Returns `true` without checking anything when no secret is set. |
| `DeliverEventAsync(WebhookSubscription, object, string, Func<int, Exception, TimeSpan, Task>?)` | POSTs the JSON-serialized event to the subscription URL, with retries. Returns a `WebhookDeliveryResult`. |

### WebhookSubscription

`WebhookSubscription` describes one endpoint. `Url` is required; the other properties have defaults.

| Property | Default | Description |
|----------|---------|-------------|
| `Id` | `null` (set on `Subscribe`) | Unique identifier used by `Unsubscribe` and `UpdateSubscription`. |
| `Url` | required | Target endpoint. Must be an `http` or `https` URL that is not loopback or private (see below). |
| `EventTypes` | empty list | Event type names to receive. Use `"*"` to receive all events. |
| `Headers` | empty dictionary | Extra HTTP headers sent with every delivery. |
| `IsActive` | `true` | Inactive subscriptions are skipped by `GetWebhooksForEvent`. |
| `MaxRetryAttempts` | `3` | Number of retries after the first attempt. |
| `InitialRetryDelay` | 1 second | Delay before the first retry. |
| `RetryBackoffMultiplier` | `2.0` | Multiplier applied to the delay after each retry. |
| `MaxRetryDelay` | 5 minutes | Upper bound for any single retry delay. |
| `UseJitter` | `true` | Adds roughly ±10% random variation to each delay. |
| `CreatedAt` | UTC now | Creation time. |
| `LastDeliveryAt`, `LastDeliveryStatus`, `LastDeliveryStatusCode`, `LastError` | `null` | Updated by `DeliverEventAsync` after each attempt. `LastDeliveryStatus` is `"Success"` on success. |

### Delivery Behavior

`DeliverEventAsync` sends a `POST` with a JSON body (`Content-Type: application/json`). Each request also carries an `X-Event-Type` header and any entries from `subscription.Headers`.

- **Retries**: Retries on HTTP 5xx, HTTP 408, network errors, and timeouts. Other 4xx responses are not retried.
- **Redirects**: Automatic redirects are disabled. A 3xx response counts as a failure and is not retried.
- **URL validation**: Only `http` and `https` are allowed. Literal IP addresses in loopback, RFC 1918, link-local, or IPv6 unique-local/link-local ranges are rejected, as is `localhost`. Host names are not resolved, so a DNS name that points to a private address is not blocked.
- **Signatures**: `DeliverEventAsync` does not attach a signature. If the receiver needs one, compute it with `GenerateSignature` over the serialized payload and send it in a header yourself.
- **Success result**: The returned `WebhookDeliveryResult` has `Success = true` and `StatusCode = 0`. Read `subscription.LastDeliveryStatusCode` for the actual HTTP status.
- **Failure**: Failures throw `HandlerInvocationException` after retries are exhausted or when a non-retryable error occurs. Its `RetryAttempts` property holds the configured `MaxRetryAttempts`, not the number of attempts actually made. Invalid URLs also surface this way, with the `ArgumentException` as `InnerException`. Null arguments and a null or whitespace `eventType` throw `ArgumentNullException` or `ArgumentException` before any request is sent.
- **Optional callback**: `onRetry` receives the 1-based retry number, the exception that triggered the retry, and the delay before the retry.

### WebhookDeliveryResult

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether delivery succeeded. |
| `ErrorMessage` | `string?` | Error message, if any. Always `null` on the success path. |
| `StatusCode` | `int?` | `0` on the success path (see above). |
| `Exception` | `Exception?` | Exception, if any. Always `null` on the success path. |

### Example Usage

```csharp
using DotnetEventBus.Integration;
using Microsoft.Extensions.Logging;

var handler = new WebhookHandler(
    signingSecret: Environment.GetEnvironmentVariable("WEBHOOK_SECRET"),
    logger: loggerFactory.CreateLogger<WebhookHandler>());

// Register an endpoint for order events
var subscription = new WebhookSubscription
{
    Url = "https://partner.example.com/hooks/orders",
    EventTypes = new List<string> { "OrderCreated", "OrderShipped" },
    Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer <token>" },
    MaxRetryAttempts = 5
};
handler.Subscribe(subscription);

// Find the endpoints that should receive an event
foreach (var target in handler.GetWebhooksForEvent("OrderCreated").ToList())
{
    try
    {
        var result = await handler.DeliverEventAsync(
            target,
            new { OrderId = "ORD-2026-001", Amount = 299.99m },
            "OrderCreated",
            onRetry: (attempt, ex, delay) =>
            {
                Console.WriteLine($"Retry {attempt} in {delay.TotalSeconds:F1}s: {ex.Message}");
                return Task.CompletedTask;
            });

        Console.WriteLine($"Delivered: {result.Success}, last status: {target.LastDeliveryStatusCode}");
    }
    catch (HandlerInvocationException ex)
    {
        Console.WriteLine($"Delivery failed after {ex.RetryAttempts} configured retries: {ex.Message}");
    }
}

// Verify an inbound signature (receiver side)
bool isAuthentic = handler.VerifySignature(requestBodyJson, signatureHeaderValue);
```

## WebhookHandlerJsonExtensions

The `WebhookHandlerJsonExtensions` class provides System.Text.Json serialization and deserialization extensions for `WebhookHandler` and `WebhookSubscription` types. It enables easy conversion between object instances and JSON strings with support for both compact and indented formatting, making it ideal for webhook payload serialization and API communication.

Example usage:

```csharp
using DotnetEventBus.Integration;
using System;

// Create a webhook handler
var handler = new WebhookHandler
{
    Id = Guid.NewGuid(),
    Name = "Order Processing Webhook",
    Url = "https://api.example.com/webhooks/order-processed",
    Secret = "whsec_test_secret_12345",
    Events = new[] { "order.created", "order.paid" },
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};

// Serialize to JSON (compact format)
string json = handler.ToJson();
Console.WriteLine(json);

// Serialize to pretty-printed JSON
string prettyJson = handler.ToJson(indented: true);
Console.WriteLine(prettyJson);

// Deserialize from JSON
var deserializedHandler = WebhookHandlerJsonExtensions.FromJson(json);
Console.WriteLine($"Deserialized handler: {deserializedHandler?.Name}");

// Try to deserialize with error handling
if (WebhookHandlerJsonExtensions.TryFromJson(json, out var safeHandler))
{
    Console.WriteLine("Successfully deserialized handler");
}

// Work with WebhookSubscription
var subscription = new WebhookSubscription
{
    Id = Guid.NewGuid(),
    HandlerId = handler.Id,
    Status = "active",
    CreatedAt = DateTime.UtcNow,
    LastDelivery = null
};

// Serialize subscription to JSON
string subscriptionJson = subscription.ToJson();

// Deserialize subscription from JSON
var deserializedSubscription = WebhookHandlerJsonExtensions.FromJsonToSubscription(subscriptionJson);
Console.WriteLine($"Deserialized subscription: {deserializedSubscription?.Status}");
```

The example includes:
- Individual event publishing for baseline comparison
- Batch event publishing with configurable batch sizes
- Performance metrics and throughput calculations
- Memory efficiency demonstration
- Multiple batching strategies for optimization

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Examples;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection with event bus
var services = new ServiceCollection();
services.AddEventBus(options => {
    options.AllowParallelHandling = true;
    options.MaxConcurrentHandlers = Environment.ProcessorCount;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
var batchPublisher = serviceProvider.GetRequiredService<IBatchEventPublisher>();

// Method 1: Individual Publishing (baseline)
for (int i = 0; i < 1000; i++)
{
    var logEvent = new BatchPublishingOptimizationExample.LogEntryEvent
    {
        LogId = $"LOG-{i:D5}",
        Level = "Info",
        Message = $"Application event {i}",
        Timestamp = DateTime.UtcNow
    };
    
    await eventBus.PublishAsync(logEvent);
}

// Method 2: Batch Publishing (optimized)
for (int i = 0; i < 1000; i++)
{
    var analyticsEvent = new BatchPublishingOptimizationExample.AnalyticsEvent
    {
        EventType = "PageView",
        UserId = $"USER-{i % 100:D3}",
        Properties = new Dictionary<string, object>
        {
            { "page", $"/page{i % 10}" },
            { "timestamp", DateTime.UtcNow },
            { "duration", Random.Shared.Next(100, 5000) }
        }
    };
    
    await batchPublisher.AddEventAsync(analyticsEvent);
    
    // Flush every 100 events
    if ((i + 1) % 100 == 0)
    {
        await batchPublisher.FlushAsync();
    }
}

// Final flush to ensure all events are published
await batchPublisher.FlushAsync();

// Get processed counts from handlers
var logCount = BatchPublishingOptimizationExample.LogAggregatorHandler.GetProcessedCount();
var analyticsCount = BatchPublishingOptimizationExample.AnalyticsProcessorHandler.GetProcessedCount();

Console.WriteLine($"Log events processed: {logCount}");
Console.WriteLine($"Analytics events processed: {analyticsCount}");
```

## OrderCreatedEvent

The `OrderCreatedEvent` class represents an event that is raised when a new order is created in the system. This event carries essential order information needed for downstream processing, including the order ID, customer details, and financial data.

### Key Properties

- **EventId**: Unique identifier for this event instance (auto-generated)
- **Timestamp**: When the event was created (UTC)
- **OrderId**: Unique identifier for the order
- **CustomerId**: Unique identifier for the customer who placed the order
- **Amount**: Total monetary amount of the order
- **ProductName**: Name of the product being ordered

### Example Usage

```csharp
using DotnetEventBus;
using DotnetEventBus.Examples.V2BasicUsage;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection with event bus
var services = new ServiceCollection();
services.AddEventBus(options => 
{
    options.AllowParallelHandling = true;
    options.MaxConcurrentHandlers = 4;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Subscribe a handler to process order created events
var subscription = eventBus.Subscribe<OrderCreatedEvent>(async (@event, ct) => 
{
    Console.WriteLine($"New order received: Order ID {@event.OrderId}");
    Console.WriteLine($"Customer: {@event.CustomerId}, Amount: ${@event.Amount}");
    Console.WriteLine($"Product: {@event.ProductName}");
    await Task.CompletedTask;
}, handlerName: "OrderProcessingHandler");

// Create and publish an order created event
var orderEvent = new OrderCreatedEvent
{
    OrderId = "ORD-2026-001",
    CustomerId = "CUST-12345",
    Amount = 299.99m,
    ProductName = "Premium Wireless Headphones"
};

var result = await eventBus.PublishAsync(orderEvent);
Console.WriteLine($"Order event published. Handlers invoked: {result.HandlersInvoked}");
```

## RequestReplyPatternExample

The `RequestReplyPatternExample` class demonstrates synchronous request-response communication using events, implementing the request-reply pattern for querying user data, product availability, and pricing information. This pattern allows services to send requests and wait for responses, enabling real-time lookups and calculations.

The example shows how to define request/response pairs, set up handlers for each request type, and make synchronous requests with timeout handling.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Examples;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection with event bus
var services = new ServiceCollection();
services.AddEventBus(options => {
    options.DefaultHandlerTimeout = TimeSpan.FromSeconds(10);
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Setup request handlers for different request types
RequestReplyPatternExample.SetupUserRequestHandler(eventBus);
RequestReplyPatternExample.SetupProductAvailabilityHandler(eventBus);
RequestReplyPatternExample.SetupPricingHandler(eventBus);

// Make a user lookup request
var userResponse = await eventBus.RequestAsync<RequestReplyPatternExample.GetUserRequest, RequestReplyPatternExample.UserResponse>(
    new RequestReplyPatternExample.GetUserRequest { UserId = "user-001" },
    timeout: TimeSpan.FromSeconds(5)
);

Console.WriteLine($"User found: {userResponse.Name} ({userResponse.Email}) - Status: {userResponse.Status}");

// Check product availability
var availabilityResponse = await eventBus.RequestAsync<RequestReplyPatternExample.GetProductAvailabilityRequest, RequestReplyPatternExample.ProductAvailabilityResponse>(
    new RequestReplyPatternExample.GetProductAvailabilityRequest { ProductId = "prod-001" },
    timeout: TimeSpan.FromSeconds(5)
);

Console.WriteLine($"Product {availabilityResponse.ProductId} available: {availabilityResponse.AvailableUnits} units at {availabilityResponse.Warehouse}");

// Calculate pricing with customer tier discount
var pricingResponse = await eventBus.RequestAsync<RequestReplyPatternExample.GetPricingRequest, RequestReplyPatternExample.PricingResponse>(
    new RequestReplyPatternExample.GetPricingRequest {
        ProductId = "prod-003",
        Quantity = 10,
        CustomerTier = "Gold"
    },
    timeout: TimeSpan.FromSeconds(5)
);

Console.WriteLine($"Total price: ${pricingResponse.TotalPrice:F2} (${pricingResponse.UnitPrice:F2} x {10} - {pricingResponse.Discount:F1}% discount: {pricingResponse.DiscountReason})");
```

## EventMessageModelTestsValidation

The `EventMessageModelTestsValidation` class provides validation extension methods for `EventMessage` and `Subscription` model classes. It offers validation methods to check model validity, return detailed validation errors, and throw exceptions when models are invalid. This is useful for unit testing and ensuring event message and subscription integrity.

Example usage:

```csharp
using DotnetEventBus.Models;
using DotnetEventBus.Tests;

// Validate an EventMessage instance
var validMessage = new EventMessage("OrderCreated", "{\"orderId\": 123}")
{
    MessageId = Guid.NewGuid().ToString(),
    CreatedAtUtc = DateTime.UtcNow
};

var messageErrors = validMessage.ValidateEventMessage();
if (validMessage.IsValid())
{
    Console.WriteLine("EventMessage is valid");
}
validMessage.EnsureValid(); // No-op if valid

// Validate a Subscription instance
var validSubscription = new Subscription
{
    Id = Guid.NewGuid().ToString(),
    EventType = "OrderCreated",
    HandlerName = "OrderHandler",
    Handler = null, // Would be set in real usage
    Priority = 5,
    CreatedAtUtc = DateTime.UtcNow
};

var subscriptionErrors = validSubscription.ValidateSubscription();
if (validSubscription.IsValid())
{
    Console.WriteLine("Subscription is valid");
}
validSubscription.EnsureValid(); // No-op if valid

// Example with invalid data to show error handling
var invalidMessage = new EventMessage("", "");
var problems = invalidMessage.ValidateEventMessage();
Console.WriteLine($"Validation found {problems.Count} problems:");
foreach (var problem in problems)
{
    Console.WriteLine($"- {problem}");
}
```

## EventBusApiControllerValidation

The `EventBusApiControllerValidation` class provides validation extension methods for API controller types and their return values in the DotnetEventBus REST API. It offers validation for `EventBusApiController`, `ApiResponse<T>`, `EventPublishResult`, `BatchPublishResult`, `EventBusStats`, and `HealthStatus` types, ensuring data consistency and proper state validation.

Example usage:

```csharp
using DotnetEventBus.Api;
using Microsoft.AspNetCore.Mvc;

// Validate an EventBusApiController instance
var controller = new EventBusApiController(eventBus);
var validationErrors = controller.Validate();
if (!controller.IsValid())
{
    Console.WriteLine("Controller validation failed!");
}
controller.EnsureValid(); // Throws if invalid

// Validate an API response
var successResponse = new ApiResponse<string>("Success", data: "result-data", timestamp: DateTime.UtcNow);
var responseErrors = successResponse.Validate();
if (successResponse.IsValid())
{
    Console.WriteLine("Response is valid");
}
successResponse.EnsureValid(); // Throws if invalid

// Validate an event publish result
var publishResult = new EventPublishResult
{
    EventType = "OrderCreated",
    PublishedAt = DateTime.UtcNow,
    Success = true
};
var publishErrors = publishResult.Validate();
if (publishResult.IsValid())
{
    Console.WriteLine("Publish result is valid");
}
publishResult.EnsureValid(); // Throws if invalid

// Validate batch publish result
var batchResult = new BatchPublishResult
{
    BatchId = Guid.NewGuid().ToString(),
    EventCount = 10,
    PublishedAt = DateTime.UtcNow,
    Success = true
};
var batchErrors = batchResult.Validate();
if (batchResult.IsValid())
{
    Console.WriteLine("Batch publish result is valid");
}
batchResult.EnsureValid(); // Throws if invalid

// Validate event bus statistics
var stats = new EventBusStats
{
    Status = "Healthy",
    TotalEventsPublished = 1000,
    TotalEventsFailed = 5,
    ActiveSubscriptions = 25,
    LastCheckTime = DateTime.UtcNow
};
var statsErrors = stats.Validate();
if (stats.IsValid())
{
    Console.WriteLine("Stats are valid");
}
stats.EnsureValid(); // Throws if invalid

// Validate health status
var healthStatus = HealthStatus.Healthy;
var healthErrors = healthStatus.Validate();
if (healthStatus.IsValid())
{
    Console.WriteLine("Health status is valid");
}
healthStatus.EnsureValid(); // No-op since all enum values are valid
```

## RequestReplyBenchmarksExtensions

The `RequestReplyBenchmarksExtensions` class provides extension methods for the `RequestReplyBenchmarks` type, offering convenience methods for running benchmarks and validating benchmark infrastructure. These methods simplify common benchmarking scenarios including running individual benchmarks, executing all benchmarks sequentially, and validating that the request-reply infrastructure is properly configured.

Example usage:

```csharp
using DotnetEventBus.Benchmarks;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection with event bus
var services = new ServiceCollection();
services.AddEventBus(options => {
    options.DefaultHandlerTimeout = TimeSpan.FromSeconds(10);
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Create benchmarks instance
var benchmarks = new RequestReplyBenchmarks(eventBus);

// Run a single benchmark by name
await benchmarks.RunSingleBenchmarkAsync(nameof(RequestReplyBenchmarks.Request_Reply_Single));

// Run all benchmarks sequentially and get execution tasks
var allBenchmarksResults = benchmarks.RunAllBenchmarksSequentially();

// Run a batch of sequential request-reply operations
await benchmarks.Request_Reply_Batch_Sequential(batchSize: 50);

// Run a batch of parallel request-reply operations
await benchmarks.Request_Reply_Batch_Parallel(concurrencyLevel: 10);

// Validate that the benchmark infrastructure is properly configured
await benchmarks.ValidateBenchmarkInfrastructureAsync(testRequestId: "validation-test");
```

## EventFilteringExample
var services = new ServiceCollection();
services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 3;
    options.RetryDelayMultiplier = 2.0;
    options.InitialRetryDelayMs = 100;
    options.EnableDeadLetterQueue = true;
    options.AllowParallelHandling = false;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
var dlqService = serviceProvider.GetRequiredService<IDeadLetterService>();

// Register handlers
eventBus.Subscribe<DeadLetterQueueHandlingExample.NotificationEvent>(
    async (@event, ct) => await new DeadLetterQueueHandlingExample.NotificationHandler().Handle(@event, ct),
    handlerName: "NotificationHandler"
);

// Publish events
await eventBus.PublishAsync(new DeadLetterQueueHandlingExample.PaymentProcessingEvent
{
    OrderId = "ORD-001",
    Amount = 99.99m,
    PaymentMethod = "Credit Card"
});

// Check dead letter queue
var deadLetterEntries = await dlqService.GetPendingEntriesAsync();

// Reprocess failed events
foreach (var entry in deadLetterEntries.Take(2))
{
    await dlqService.ReprocessEntryAsync(entry.Id);
}

// Get statistics
var stats = await dlqService.GetStatisticsAsync();
Console.WriteLine($"Pending entries: {stats.PendingEntries}");
```

## EventFilteringExample

The `EventFilteringExample` class demonstrates selective event handler execution based on event properties using fluent filter APIs. It shows how to create filtered subscriptions that only process events matching specific criteria, enabling targeted event handling for different business scenarios.

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Advanced;
using Microsoft.Extensions.DependencyInjection;

// Create service collection and configure event bus
var services = new ServiceCollection();
services.AddEventBus(options => {
    options.AllowParallelHandling = false;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Define event types
public sealed class SalesEvent
{
    public string OrderId { get; set; }
    public string Region { get; set; }
    public decimal Amount { get; set; }
    public string CustomerSegment { get; set; }
    public DateTime Timestamp { get; set; }
}

public sealed class AlertEvent
{
    public string AlertId { get; set; }
    public string Severity { get; set; }
    public string Source { get; set; }
    public string Message { get; set; }
}

// Create a filter for high-value orders (> $1000)
var highValueFilter = new EventFilterBuilder()
    .Where<SalesEvent>(e => e.Amount > 1000m)
    .Build();

// Subscribe a handler that only processes high-value orders
eventBus.Subscribe<SalesEvent>(
    async (@event, ct) => {
        Console.WriteLine($"Processing high-value order: {0}", @event.OrderId);
        await Task.CompletedTask;
    },
    handlerName: "HighValueOrderHandler",
    filter: highValueFilter
);

// Subscribe a handler for premium customers
var premiumFilter = new EventFilterBuilder()
    .Where<SalesEvent>(e => e.CustomerSegment == "Premium")
    .Build();

eventBus.Subscribe<SalesEvent>(
    async (@event, ct) => {
        Console.WriteLine($"Processing premium customer order");
        await Task.CompletedTask;
    },
    handlerName: "PremiumCustomerHandler",
    filter: premiumFilter
);

// Subscribe a handler for critical alerts
var criticalFilter = new EventFilterBuilder()
    .Where<AlertEvent>(e => e.Severity == "Critical")
    .Build();

eventBus.Subscribe<AlertEvent>(
    async (@event, ct) => {
        Console.WriteLine($"CRITICAL ALERT: {0}", @event.Message);
        await Task.CompletedTask;
    },
    handlerName: "CriticalAlertHandler",
    filter: criticalFilter
);

// Publish events - only matching handlers will be invoked
await eventBus.PublishAsync(new SalesEvent {
    OrderId = "ORD-123",
    Region = "North America",
    Amount = 1500m,
    CustomerSegment = "Premium",
    Timestamp = DateTime.UtcNow
});

await eventBus.PublishAsync(new AlertEvent {
    AlertId = "ALT-456",
    Severity = "Critical",
    Source = "Database",
    Message = "Connection pool exhausted"
});
```

## MetricsCollector

`MetricsCollector` (namespace `DotnetEventBus.Advanced`) records event publication, failure, handler execution, latency, throughput, and uptime metrics in memory. Create it directly, optionally passing an `ILogger<MetricsCollector>`, and call its recording methods from the event publication and handler paths you want to observe. Durations are expressed in milliseconds and success rates are percentages from `0` to `100`.

Public API:

- `MetricsCollector(ILogger<MetricsCollector>? logger = null)` creates an empty collector with optional diagnostic logging.
- `RecordEventPublished(string eventType, long durationMs)` records a publication and its duration.
- `RecordEventFailed(string eventType, string handlerName, Exception exception)` records an event failure and associates it with a handler.
- `RecordHandlerExecution(string handlerName, string eventType, long durationMs, bool success)` records a handler invocation.
- `GetEventMetrics(string eventType)` returns `EventMetrics` for one event type, or `null` when it has not been recorded.
- `GetAllEventMetrics()` returns all event metrics ordered by descending publication count.
- `GetHandlerMetrics(string handlerName)` returns that handler's metrics for every recorded event type, ordered by descending execution count.
- `GetHandlerMetrics(string handlerName, string eventType)` returns one handler/event pair, or `null` when it has not been recorded.
- `GetAllHandlerMetrics()` returns every handler/event pair ordered by descending execution count.
- `GetSuccessRate(string handlerName, string eventType)` and `GetAverageDuration(string handlerName, string eventType)` return `0` when the pair has not been recorded.
- `GetSystemMetrics()` returns totals, success rate, event and handler counts, uptime, and throughput per second.
- `GetLatencyStats(string eventType)` returns minimum, maximum, average, and p95 latency, or `null` when the event type has not been recorded. The p95 calculation uses up to the 1,000 most recent duration samples.
- `GetAllLatencyStats()` returns latency statistics for all recorded event types.
- `Reset()` clears all collected data and restarts the uptime measurement.
- `ToString()` returns a concise diagnostic summary of the current state.

The returned public models expose the following data:

- `EventMetrics`: `EventType`, publication and failure counts, total/average/minimum/maximum duration, last publication and failure timestamps, last error, and computed `SuccessRate`.
- `HandlerMetrics`: `HandlerName`, `EventType`, execution/failure/total-duration/average-duration values, and computed `SuccessCount` and `SuccessRate`.
- `SystemMetrics`: start time, uptime, published and failed totals, success rate, event-type and handler counts, and throughput per second.
- `LatencyStats`: event type, minimum/maximum/average/p95 latency in milliseconds, and total recorded `SampleCount`.

Example usage:

```csharp
using DotnetEventBus.Advanced;

var metrics = new MetricsCollector();

// Record the publication and each handler execution independently.
metrics.RecordEventPublished("OrderCreated", durationMs: 150);
metrics.RecordHandlerExecution(
    handlerName: "OrderCreatedHandler",
    eventType: "OrderCreated",
    durationMs: 120,
    success: true);

metrics.RecordEventPublished("OrderCreated", durationMs: 210);
metrics.RecordHandlerExecution(
    handlerName: "AuditHandler",
    eventType: "OrderCreated",
    durationMs: 25,
    success: false);
metrics.RecordEventFailed(
    eventType: "OrderCreated",
    handlerName: "AuditHandler",
    exception: new InvalidOperationException("Audit store unavailable"));

EventMetrics? orderMetrics = metrics.GetEventMetrics("OrderCreated");
if (orderMetrics is not null)
{
    Console.WriteLine(
        $"Published: {orderMetrics.PublishCount}; " +
        $"failures: {orderMetrics.FailureCount}; " +
        $"average: {orderMetrics.AverageDurationMs:F1} ms");
}

HandlerMetrics? auditMetrics = metrics.GetHandlerMetrics("AuditHandler", "OrderCreated");
Console.WriteLine($"Audit success rate: {metrics.GetSuccessRate("AuditHandler", "OrderCreated"):F1}%");
Console.WriteLine($"Audit average duration: {metrics.GetAverageDuration("AuditHandler", "OrderCreated"):F1} ms");

foreach (HandlerMetrics handler in metrics.GetHandlerMetrics("OrderCreatedHandler"))
{
    Console.WriteLine($"{handler.EventType}: {handler.ExecutionCount} executions");
}

LatencyStats? latency = metrics.GetLatencyStats("OrderCreated");
if (latency is not null)
{
    Console.WriteLine($"Latency p95: {latency.P95Ms} ms ({latency.SampleCount} publications)");
}

SystemMetrics system = metrics.GetSystemMetrics();
Console.WriteLine(
    $"Published: {system.TotalEventsPublished}; " +
    $"throughput: {system.ThroughputPerSecond:F2}/s; uptime: {system.UpTime}");

IEnumerable<EventMetrics> events = metrics.GetAllEventMetrics();
IEnumerable<HandlerMetrics> handlers = metrics.GetAllHandlerMetrics();
IEnumerable<LatencyStats> latencies = metrics.GetAllLatencyStats();

Console.WriteLine(metrics);
metrics.Reset();
```

## HealthCheck

`HealthCheck` (namespace `DotnetEventBus.Monitoring`) runs a set of named `IHealthCheckProbe` instances and combines their results into one report. Register each probe with a name, call `CheckHealthAsync()` to run them all, and read the returned `HealthCheckResult`. The probes run one after another, and the class is not an ASP.NET Core health check, so the result can be exposed through any endpoint or logging you choose.

The overall status is `Healthy` only when every probe reports `Healthy`. Any other probe status (`Degraded` or `Unknown` included) makes the overall status `Unhealthy`, and a probe that throws is recorded as `Unhealthy` with the exception message. `CheckHealthAsync()` does not throw because of a probe failure. With no probes registered, the overall status is `Healthy`.

Public API:

- `void RegisterProbe(string name, IHealthCheckProbe probe)` adds a probe under `name`. Both arguments must be non-null (`ArgumentNullException` otherwise). Registering the same name again replaces the earlier probe.
- `Task<HealthCheckResult> CheckHealthAsync()` runs every registered probe and returns the aggregate result. It also stores the overall status and check time for `GetLastStatus()` and `GetLastCheckTime()`.
- `HealthStatus GetLastStatus()` returns the overall status of the most recent `CheckHealthAsync()` call, or `Unknown` if no check has run yet. It does not run probes.
- `DateTime GetLastCheckTime()` returns the UTC time of the most recent check, or `DateTime.MinValue` if no check has run yet.
- `ToString()` returns the last status, last check time, and number of registered probes.

Types:

- `IHealthCheckProbe` has one member, `Task<ProbeResult> CheckAsync()`. Implement it to measure a component.
- `ProbeResult` contains `Status`, an optional `Message`, and a `Details` dictionary of additional values. `Status` defaults to `Healthy`, so set it explicitly.
- `HealthCheckResult` contains `OverallStatus`, `CheckedAt` (UTC), and `Checks`, a dictionary of each `ProbeResult` keyed by probe name.
- `HealthStatus` has the values `Healthy`, `Degraded`, `Unhealthy`, and `Unknown`. `GetLastStatus()` returns `Unknown` before the first check. `OverallStatus` is only ever `Healthy` or `Unhealthy`, although individual probes may report `Degraded`.

`BuiltInProbes` (same namespace) provides two ready-made probes:

- `BuiltInProbes.CreateMemoryProbe(long warningThresholdBytes = 1_073_741_824)` reports `Degraded` when `GC.GetTotalMemory(false)` is above the threshold. Its `Details` contain `memoryBytes` and `warningThresholdBytes`. The probe measures managed heap only, not the process's total working set.
- `BuiltInProbes.CreateResponsivenessProbe()` waits 10 ms with `Task.Delay` and reports `Degraded` when that takes more than 100 ms. Its `Details` contain `latencyMs`. This probe measures timer and scheduler latency; it does not exercise the event bus itself, so write a custom probe if you need a bus-level check.

Example usage:

```csharp
using DotnetEventBus.Monitoring;

var health = new HealthCheck();

health.RegisterProbe("memory", BuiltInProbes.CreateMemoryProbe(512L * 1024 * 1024));
health.RegisterProbe("responsiveness", BuiltInProbes.CreateResponsivenessProbe());
health.RegisterProbe("order-queue", new OrderQueueProbe(() => orderQueue.Count));

HealthCheckResult result = await health.CheckHealthAsync();

Console.WriteLine($"Overall: {result.OverallStatus} at {result.CheckedAt:O}");
foreach (var (name, probe) in result.Checks)
{
    Console.WriteLine($"{name}: {probe.Status} - {probe.Message}");
}

Console.WriteLine(health.GetLastStatus());
```

A custom probe that reports a backlog size:

```csharp
using DotnetEventBus.Monitoring;

public sealed class OrderQueueProbe : IHealthCheckProbe
{
    private readonly Func<int> _backlog;

    public OrderQueueProbe(Func<int> backlog)
    {
        _backlog = backlog;
    }

    public Task<ProbeResult> CheckAsync()
    {
        int backlog = _backlog();

        return Task.FromResult(new ProbeResult
        {
            Status = backlog > 1000 ? HealthStatus.Degraded : HealthStatus.Healthy,
            Message = $"Backlog: {backlog}",
            Details = new Dictionary<string, object> { ["backlog"] = backlog }
        });
    }
}
```

## CircuitBreaker

`CircuitBreaker` (namespace `DotnetEventBus.Integration`) protects asynchronous calls to a dependency that is repeatedly failing. It begins in the `Closed` state and records every exception thrown by the wrapped operation. After the configured number of consecutive failures, it becomes `Open` and rejects calls until the timeout has elapsed. The next call after the timeout changes the state to `HalfOpen`: a successful call closes the circuit, while a failed call immediately reopens it. Any successful call resets the accumulated failure count.

Public API:

- `CircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null)` creates a closed circuit. The default timeout is 60 seconds. `failureThreshold` must be greater than zero; otherwise, the constructor throws `ArgumentException`.
- `State` returns the current `CircuitBreakerState` in a thread-safe manner.
- `ExecuteAsync<T>(Func<Task<T>> operation)` runs an asynchronous operation and returns its result.
- `ExecuteAsync(Func<Task> operation)` runs an asynchronous operation that has no result.
- Both `ExecuteAsync` overloads throw `ArgumentNullException` for a null operation, propagate exceptions from the operation after recording the failure, and throw `CircuitBreakerOpenException` without invoking the operation when the circuit is still open.
- `Reset()` manually changes the state to `Closed` and clears the accumulated failure count.

Supporting public types:

- `CircuitBreakerState` defines `Closed` (calls are allowed), `Open` (calls are rejected), and `HalfOpen` (recovery is being tested).
- `CircuitBreakerOpenException` is thrown when a call is rejected by an open circuit. Its constructor accepts the exception message.

Example usage:

```csharp
using DotnetEventBus.Integration;

var breaker = new CircuitBreaker(
    failureThreshold: 2,
    timeout: TimeSpan.FromSeconds(30));

async Task<string> LoadOrderAsync()
{
    // Call the protected dependency here.
    await Task.Delay(10);
    return "order-42";
}

try
{
    string order = await breaker.ExecuteAsync(LoadOrderAsync);

    // The overload without a result is useful for command-style operations.
    await breaker.ExecuteAsync(() => Task.CompletedTask);
}
catch (CircuitBreakerOpenException)
{
    // The operation was not invoked; use a fallback or return a temporary error.
}
catch (Exception)
{
    // The operation failed. Its original exception is propagated to the caller.
}

Console.WriteLine(breaker.State);

// After the timeout, the next ExecuteAsync call tests recovery in HalfOpen state.
// Call Reset() only when the circuit should be closed manually.
breaker.Reset();
```

## RetryPolicy

`RetryPolicy` (namespace `DotnetEventBus.Integration`) runs an asynchronous operation again after it fails with a retryable exception. It is separate from `EventBus`, which uses only the retry settings in `EventBusOptions` (see [Retries](#retries)). Use `RetryPolicy` to wrap calls to external dependencies, such as an HTTP request made from a handler.

Public API:

- `RetryPolicy()` creates a policy with these defaults: 3 retries, a 1 second initial delay, a backoff multiplier of 2.0, a 5 minute maximum delay, jitter enabled, and the default exception filter.
- `WithMaxRetries(int maxRetries)` sets the number of retries after the first attempt. `0` means a single attempt. A negative value throws `ArgumentException`.
- `WithInitialDelay(TimeSpan delay)` sets the delay before the first retry. `TimeSpan.Zero` is allowed. A negative value throws `ArgumentException`.
- `WithBackoffMultiplier(double multiplier)` sets the factor applied to the delay after each retry. The value must be greater than `1.0`; otherwise, it throws `ArgumentException`.
- `WithMaxDelay(TimeSpan maxDelay)` caps the delay before jitter. The value must be positive; otherwise, it throws `ArgumentException`.
- `WithJitter(bool enabled)` turns the ±10% random variation of each delay on or off. It does not validate its input.
- `WithRetryableExceptionFilter(Func<Exception, bool>? filter)` replaces the default filter. Passing `null` restores the default.
- Every `With*` method changes the policy in place and returns the same instance, so calls can be chained. Configure the policy before it is used to run operations.
- `ExecuteAsync<T>(Func<Task<T>> operation, Func<int, Exception, TimeSpan, Task>? onRetry = null)` runs the operation and returns its result.
- `ExecuteAsync(Func<Task> operation, Func<int, Exception, TimeSpan, Task>? onRetry = null)` runs an operation that has no result.
- Both `ExecuteAsync` overloads throw `ArgumentNullException` when `operation` is null.

Retry behavior:

- The default filter retries only `TimeoutException` and `InvalidOperationException`. A custom filter replaces this list entirely.
- When an attempt fails with a retryable exception and retries remain, the policy calls `onRetry` if one was provided. It passes the 1-based retry number, the exception, and the delay. It then waits and runs the operation again.
- When the exception is not retryable, or the retries are used up, the original exception is rethrown unchanged.
- An exception thrown by `onRetry` is not caught. It goes to the caller, and no further retries run.
- The delay before retry `n`, counting from 0, is `InitialDelay × BackoffMultiplier^n`, capped at `MaxDelay`. When jitter is on, the capped value is then varied by ±10%, so a delay can be up to 10% longer than `MaxDelay`.
- The policy has no `CancellationToken` parameter, and the wait between attempts cannot be cancelled. To stop retrying on cancellation, make the filter return `false` for `OperationCanceledException`. The operation itself still receives any token you pass to it through a closure.

Factory methods are available on the static `RetryPolicyBuilder` class:

- `RetryPolicyBuilder.CreateDefault()` returns `new RetryPolicy()`.
- `RetryPolicyBuilder.CreateExponentialBackoff(int maxRetries = 3)` uses a 1 second initial delay and a multiplier of 2.0.
- `RetryPolicyBuilder.CreateLinearBackoff(int maxRetries = 3)` uses a 1 second initial delay and a multiplier of 1.5. Despite the name, the delay still grows geometrically, not linearly.
- `RetryPolicyBuilder.CreateImmediate(int maxRetries = 3)` uses a zero initial delay, so retries start without waiting.

Example usage:

```csharp
using DotnetEventBus.Integration;

var retry = new RetryPolicy()
    .WithMaxRetries(4)
    .WithInitialDelay(TimeSpan.FromMilliseconds(200))
    .WithBackoffMultiplier(2.0)              // waits about 200 ms, 400 ms, 800 ms, 1.6 s
    .WithMaxDelay(TimeSpan.FromSeconds(5))
    .WithJitter(true)
    .WithRetryableExceptionFilter(ex => ex is HttpRequestException);

async Task<string> FetchOrderAsync(string id)
{
    // Call the external dependency here.
    await Task.Delay(10);
    return $"order-{id}";
}

// Operation with a result. onRetry is optional and runs before each wait.
string order = await retry.ExecuteAsync(
    () => FetchOrderAsync("42"),
    onRetry: (attempt, ex, delay) =>
    {
        Console.WriteLine($"Retry {attempt} after {ex.GetType().Name}, waiting {delay.TotalMilliseconds} ms");
        return Task.CompletedTask;
    });

// Operation without a result.
await retry.ExecuteAsync(() => Task.CompletedTask);

// Factory methods create a policy with common settings.
var immediate = RetryPolicyBuilder.CreateImmediate(maxRetries: 2);
await immediate.ExecuteAsync(() => FetchOrderAsync("43"));
```

## ECommerceOrderProcessingExample

The `ECommerceOrderProcessingExample` class demonstrates a complete e-commerce order processing workflow using the event bus system. It shows how to define custom events for order placement, payment processing, and shipment creation, and how to create handlers with different priorities that work together to process an order through multiple stages.

The example includes:
- Order placement with inventory reservation (high priority)
- Payment processing and transaction handling (medium priority)
- Shipment creation and tracking (triggered by payment event)
- Customer notifications (low priority)
- Event chaining and dependency injection for handler services

Example usage:

```csharp
using DotnetEventBus;
using DotnetEventBus.Examples;
using Microsoft.Extensions.DependencyInjection;

// Setup dependency injection with event bus
var services = new ServiceCollection();
services.AddEventBus(options =>
{
    options.AllowParallelHandling = false; // Sequential for clear output
    options.MaxRetryAttempts = 3;
    options.EnableDeadLetterQueue = true;
});

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();

// Create sample order
var order = new ECommerceOrderProcessingExample.OrderPlacedEvent
{
    OrderId = "ORD-2026-001",
    CustomerId = "CUST-123",
    Items = new List<ECommerceOrderProcessingExample.OrderItem>
    {
        new ECommerceOrderProcessingExample.OrderItem { ProductId = "PROD-001", Quantity = 2, UnitPrice = 49.99m },
        new ECommerceOrderProcessingExample.OrderItem { ProductId = "PROD-002", Quantity = 1, UnitPrice = 99.99m }
    },
    TotalPrice = 199.97m,
    PlacedAt = DateTime.UtcNow
};

// Publish the order
var result = await eventBus.PublishAsync(order);

Console.WriteLine($"Order processing completed: {result.HandlersInvoked} handlers invoked");
```

## EventBusExceptionTests

The `EventBusExceptionTests` class provides comprehensive unit tests for the `EventBusException` hierarchy, validating constructor behavior, property initialization, and null/empty string handling across all derived exception types. These tests ensure that custom exceptions like `NoHandlersRegisteredException`, `HandlerInvocationException`, `InvalidHandlerException`, `MessageSerializationException`, `DistributedBusNotConfiguredException`, and `RequestTimeoutException` are instantiated correctly and carry the expected diagnostic information.

Example usage:
```csharp
using DotnetEventBus.Exceptions;
using System;

try
{
    // Instantiate base exception with message and inner exception
    var baseException = new EventBusException("Event bus operation failed", new InvalidOperationException("Root cause"));
    Console.WriteLine(baseException.Message);

    // Handle scenarios where no handlers are registered
    var noHandlersException = new NoHandlersRegisteredException("OrderCreated");
    Console.WriteLine($"Event type: {noHandlersException.EventType}");

    // Handle handler invocation failures
    var invocationException = new HandlerInvocationException("OrderHandler", "OrderCreated", new TimeoutException("Handler timed out"));
    Console.WriteLine($"Handler: {invocationException.HandlerName}, Event: {invocationException.EventType}");

    // Handle invalid handler types
    var invalidHandlerException = new InvalidHandlerException(typeof(object));
    Console.WriteLine($"Invalid handler type: {invalidHandlerException.HandlerType}");

    // Handle message serialization failures
    var serializationException = new MessageSerializationException(typeof(object));
    Console.WriteLine($"Failed message type: {serializationException.MessageType}");

    // Handle distributed bus configuration errors
    var distributedException = new DistributedBusNotConfiguredException();
    Console.WriteLine(distributedException.Message);

    // Handle request timeout scenarios
    var timeoutException = new RequestTimeoutException(typeof(object), TimeSpan.FromSeconds(30));
    Console.WriteLine($"Request type: {timeoutException.RequestType}, Timeout: {timeoutException.Timeout}");
}
catch (EventBusException ex)
{
    Console.WriteLine($"Caught EventBusException: {ex.Message}");
}
```

## EventBusBuilderValidationTests

The `EventBusBuilderValidationTests` class provides comprehensive unit tests for the validation logic of the `EventBusBuilder`. It verifies that the builder correctly validates configuration options such as request timeout, retry attempts, and distributed transport settings, returning appropriate validation errors or throwing exceptions for invalid inputs.

Example usage:
```csharp
using DotnetEventBus;
using DotnetEventBus.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Create a service collection and builder
var services = new ServiceCollection();
var builder = services.AddEventBusBuilder();

// Validate a valid builder (should return empty list)
var errors = builder.Validate();
if (errors.Count == 0)
{
    Console.WriteLine("Builder is valid.");
}

// Validate with invalid options (e.g., zero request timeout)
builder.WithOptions(options => options.RequestTimeout = TimeSpan.Zero);
errors = builder.Validate();
if (errors.Count > 0)
{
    Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
}

// Using EnsureValid to throw on invalid builder
try
{
    builder.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## EventBusBuilderTests

The `EventBusBuilderTests` class provides comprehensive unit tests for the `EventBusBuilder` configuration API, verifying that all builder methods correctly apply settings, validate inputs, and support fluent chaining. It ensures that options like retry policies, timeouts, parallel handling, and custom repositories are properly registered in the service collection.

Example usage:
```csharp
using DotnetEventBus;
using DotnetEventBus.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;

var services = new ServiceCollection();
var builder = services.AddEventBusBuilder();

// Configure core options with validation
builder.WithOptions(o => o.RequestTimeout = TimeSpan.FromSeconds(30))
       .WithMaxRetries(3)
       .WithHandlerTimeout(TimeSpan.FromSeconds(10))
       .WithParallelHandling(true)
       .WithMaxConcurrentHandlers(5)
       .WithDeadLetterQueue(true);

// Register custom repositories
var mockMessageRepo = new Mock<IEventMessageRepository>();
var mockSubRepo = new Mock<ISubscriptionRepository>();
var mockDlqRepo = new Mock<IDeadLetterRepository>();

builder.WithMessageRepository(mockMessageRepo.Object)
       .WithSubscriptionRepository(mockSubRepo.Object)
       .WithDeadLetterRepository(mockDlqRepo.Object);

// Build the service collection
var serviceProvider = builder.Build().BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
```

## PredicateSubscriptionBuilderValidationTests

The `PredicateSubscriptionBuilderValidationTests` class provides comprehensive unit tests for the `PredicateSubscriptionBuilder` validation logic. It verifies that the builder correctly validates configuration constraints such as handler configuration, handler name requirements, and priority boundaries, returning appropriate validation errors or throwing exceptions for invalid inputs. The tests cover scenarios including null checks, empty or null handler names, out-of-range priorities, multiple simultaneous errors, and the read-only nature of the returned error list.

Example usage:
```csharp
using DotnetEventBus.Advanced;
using Microsoft.Extensions.DependencyInjection;

// Create a service collection and configure event bus
var services = new ServiceCollection();
services.AddEventBus();
var serviceProvider = services.BuildServiceProvider();

// Create a predicate subscription builder
var builder = new PredicateSubscriptionBuilder<MyEvent>(serviceProvider);

// Validate configuration constraints
var errors = builder.Validate();
if (errors.Count == 0)
{
    Console.WriteLine("Builder is valid.");
}

// Validate with invalid handler name
builder.WithHandlerName("");
errors = builder.Validate();
if (errors.Count > 0)
{
    Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
}

// Check validity
bool isValid = builder.IsValid();
Console.WriteLine($"Is valid: {isValid}");

// Using EnsureValid to throw on invalid builder
try
{
    builder.EnsureValid();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## EventFilter

The `EventFilter<T>` class (namespace `DotnetEventBus.Advanced`) provides a fluent API for filtering events before they are processed by handlers. It lets you register one or more predicates and compile them into a single optimized predicate function, reducing unnecessary handler invocations by filtering at the bus level. The companion `FilterBuilder` static class offers factory methods for creating filters, including wildcard (match-all) and empty (match-none) variants.

Key members:
- `Where(Func<T, bool>)` — adds a predicate filter (AND semantics).
- `WhereProperty(selector, expectedValue)` — matches when a property equals a value.
- `WherePropertyInRange(selector, min, max)` — matches when a comparable property falls within an inclusive range.
- `WherePropertyContains(selector, value)` — case-insensitive substring match on a string property.
- `Not(Func<T, bool>)` — inverts a predicate.
- `Compile()` — combines all registered predicates into a single cached predicate function (match-all when empty, the single predicate when only one is registered).
- `Matches(T)` — evaluates all filters against a single event.
- `FilterCollection(IEnumerable<T>)` — filters a collection of events.
- `FilterCount` — number of registered filters.
- `Clear()` — removes all filters and resets the compiled cache.

Example usage:
```csharp
using DotnetEventBus.Advanced;

// Build a filter that matches orders over $100 for a specific user
var filter = FilterBuilder.CreateFilter<OrderEvent>()
    .WhereProperty(e => e.UserId, 42)
    .WherePropertyInRange(e => e.Amount, 100m, 10_000m)
    .WherePropertyContains(e => e.Status, "pending");

// Evaluate a single event
bool matches = filter.Matches(new OrderEvent { UserId = 42, Amount = 250m, Status = "Pending" });

// Filter a collection of events
var filtered = filter.FilterCollection(orders);

// Compile once and reuse the predicate
Func<OrderEvent, bool> predicate = filter.Compile();
bool isMatch = predicate(order);

// Wildcard (match all) and empty (match none) helpers
var all = FilterBuilder.CreateWildcardFilter<OrderEvent>();
var none = FilterBuilder.CreateEmptyFilter<OrderEvent>();
```

## EventTransformer

The types in `DotnetEventBus.Advanced.EventTransformer.cs` provide reusable mappings between reference-type events. `EventTransformer<TSource, TTarget>` applies an initial mapping and can add ordered post-transformation steps or chain the result into another target type. `EventTransformerBuilder` supplies factory methods for custom mappings, matching-property copies, and dictionary conversion.

Public API:

- `EventTransformer(Func<TSource, TTarget>)` — creates a transformer from a mapping function.
- `Then(Func<TTarget, TTarget>)` — appends a post-transformation step and returns the same transformer for fluent configuration. Steps run in registration order.
- `Transform(TSource)` — transforms one event and applies every registered post-transformation.
- `TransformMany(IEnumerable<TSource>)` — returns a lazily evaluated sequence of transformed events.
- `Chain<TIntermediate>(Func<TTarget, TIntermediate>)` — creates a new transformer that runs the current transformation pipeline before mapping to another reference type.
- `EventTransformerBuilder.CreateTransformer<TSource, TTarget>(mapFunc)` — creates a transformer with a custom mapping.
- `EventTransformerBuilder.CreatePropertyCopyTransformer<TSource, TTarget>()` — creates a transformer that copies same-named public properties to a target with a parameterless constructor. Properties that cannot be assigned are skipped.
- `EventTransformerBuilder.CreateDictionaryTransformer<T>()` — creates a transformer that maps public property names and values into a `Dictionary<string, object?>`.

Example usage:

```csharp
using DotnetEventBus.Advanced;

var transformer = EventTransformerBuilder
    .CreateTransformer<OrderSubmitted, OrderSummary>(order => new OrderSummary
    {
        OrderId = order.OrderId,
        Total = order.Total,
        Label = order.CustomerName
    })
    .Then(summary =>
    {
        summary.Label = summary.Label.Trim();
        return summary;
    });

var notificationTransformer = transformer.Chain(summary => new OrderNotification
{
    Message = $"Order {summary.OrderId}: {summary.Label} ({summary.Total:C})"
});

OrderSummary summary = transformer.Transform(
    new OrderSubmitted { OrderId = 42, CustomerName = "  Ada  ", Total = 125m });

IEnumerable<OrderSummary> summaries = transformer.TransformMany(pendingOrders);
OrderNotification notification = notificationTransformer.Transform(pendingOrders.First());

// Copy compatible, same-named properties to a new target instance.
var copyTransformer = EventTransformerBuilder
    .CreatePropertyCopyTransformer<OrderSubmitted, OrderArchiveEntry>();
OrderArchiveEntry archiveEntry = copyTransformer.Transform(pendingOrders.First());

// Convert all public properties to name/value pairs.
var dictionaryTransformer = EventTransformerBuilder
    .CreateDictionaryTransformer<OrderSubmitted>();
Dictionary<string, object?> values = dictionaryTransformer.Transform(pendingOrders.First());
```

## SagaOrchestrator

`SagaOrchestrator<TContext>` (namespace `DotnetEventBus.Advanced`) coordinates an ordered sequence of asynchronous saga steps. Each step can define a compensation action. If a forward action fails, the orchestrator marks that step as failed and compensates the previously completed steps in reverse order. The context must be a reference type and the same instance is passed to every action and compensation.

Public API:

- `SagaOrchestrator(string sagaId, ILogger<SagaOrchestrator<TContext>>? logger = null)` creates an orchestrator with an optional logger. `sagaId` is copied to `Name` and is returned in each execution result.
- `Name` gets or sets the display name used to identify the saga. Changing it does not change the result's `SagaId`.
- `AddStep(string stepName, Func<TContext, Task> action, Func<TContext, Task>? compensationAction = null)` appends a step and returns the orchestrator for fluent chaining. New steps start with `Pending` status.
- `ExecuteAsync(TContext context)` executes steps in registration order. It returns a successful `SagaExecutionResult` when every step completes (including when no steps are registered). A forward-action failure produces an unsuccessful result after rollback. If any compensation also fails, the method throws `SagaCompensationException` containing the result and compensation failures.
- `GetStepStatus()` returns a snapshot of the registered step sequence and its current state.

Supporting public types:

- `SagaStep<T>` exposes `Name`, `Action`, optional `CompensationAction`, `Status`, and optional `ErrorMessage`.
- `SagaStepStatus` defines `Pending`, `Running`, `Completed`, `Compensating`, `Compensated`, `Failed`, and `CompensationFailed`.
- `SagaExecutionResult` exposes `SagaId`, `Success`, `FailedStep`, `Error`, and `ExecutedAt` (initialized to the current UTC time when execution begins).

Example usage:

```csharp
using DotnetEventBus.Advanced;

var saga = new SagaOrchestrator<OrderSagaContext>("order-42")
    .AddStep(
        "reserve-inventory",
        context =>
        {
            context.InventoryReserved = true;
            return Task.CompletedTask;
        },
        context =>
        {
            context.InventoryReserved = false;
            return Task.CompletedTask;
        })
    .AddStep(
        "charge-payment",
        context => Task.FromException(
            new InvalidOperationException("Payment was declined")));

var context = new OrderSagaContext();
SagaExecutionResult result = await saga.ExecuteAsync(context);

Console.WriteLine(result.Success);             // False
Console.WriteLine(result.FailedStep);          // charge-payment
Console.WriteLine(context.InventoryReserved); // False: the first step was compensated

foreach (SagaStep<OrderSagaContext> step in saga.GetStepStatus())
{
    Console.WriteLine($"{step.Name}: {step.Status}");
}

public sealed class OrderSagaContext
{
    public bool InventoryReserved { get; set; }
}
```

## RequestResponsePattern

`src/DotnetEventBus/Advanced/RequestResponsePattern.cs` provides request-response (RPC-style) communication on top of the asynchronous event bus. A caller sends a request, the bus generates a request id, and the caller awaits a matching response or a failure. The file defines four public types: `RequestResponseBus`, `RequestResponseHandler<TRequest, TResponse>`, `RequestMessage<T>`, and `ResponseMessage<T>`.

Public API of `RequestResponseBus` (namespace `DotnetEventBus.Advanced`):

- `RequestResponseBus(TimeSpan? defaultTimeout = null)` creates a bus without an outgoing publisher. The default timeout is 30 seconds. `RequestAsync` registers the request but does not send it; it is useful when responses are delivered by other code through `SendResponse`, `FailRequest`, or `AuditPendingRequests`.
- `RequestResponseBus(Func<string, RequestMessage<object>, Task> publishRequest, TimeSpan? defaultTimeout = null)` creates a bus that sends each request through `publishRequest`. The delegate receives the event type and a `RequestMessage<object>` containing the generated `RequestId`, the payload, and the effective timeout. Throws `ArgumentNullException` when `publishRequest` is null.
- `Task<TResponse?> RequestAsync<TRequest, TResponse>(string eventType, TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)` sends a request and waits for its response. Both `TRequest` and `TResponse` must be reference types. It throws `ArgumentNullException` for a null `eventType` or `request`, `TimeoutException` when no response arrives within `timeout` (or the bus default), and `OperationCanceledException` when `cancellationToken` is canceled. If the response is not assignable to `TResponse`, the method returns `null` instead of throwing.
- `bool SendResponse(string requestId, object response)` completes a pending request with a response. It returns `true` when the response was delivered and `false` when the request id is unknown or the request has already completed.
- `bool FailRequest(string requestId, Exception exception)` completes a pending request with an exception. It returns `true` when the exception was delivered and `false` when the request id is unknown or the request has already completed.
- `int GetPendingRequestCount()` returns the number of requests that are waiting for a response.
- `void CancelAllRequests(string reason = "Bus shutting down")` faults every pending request with an `OperationCanceledException` that carries `reason`. Use it during shutdown.
- `Dictionary<string, string> AuditPendingRequests()` returns each pending request id mapped to its current task status, such as `Waiting` or `Completed`. It is intended for diagnostics.
- `int CleanupCompletedRequests()` removes entries whose tasks have already completed and returns how many were removed. Normal operation removes entries automatically, so this is a defensive operation.

Public API of `RequestResponseHandler<TRequest, TResponse>`:

- `abstract Task<TResponse> HandleAsync(TRequest request)` is implemented by the subclass to produce the response.
- `Task ProcessRequestAsync(TRequest request, string requestId, RequestResponseBus bus)` runs `HandleAsync`, then calls `bus.SendResponse` with the result. If `HandleAsync` throws, it calls `bus.FailRequest` with the exception instead of rethrowing. It throws `ArgumentNullException` for null arguments.
- The protected `RequestId` and `Bus` properties are set before `HandleAsync` runs, so subclasses can read them.

Supporting public types:

- `RequestMessage<T>` (`where T : class`) has the properties `string? RequestId`, `required T Payload`, `Dictionary<string, object> Metadata` (empty by default), `DateTime CreatedAt` (UTC, set on creation), and `TimeSpan Timeout` (30 seconds by default).
- `ResponseMessage<T>` (`where T : class`) has the properties `string? RequestId`, `required T Payload`, `bool Success` (`true` by default), `string? ErrorMessage`, and `DateTime CreatedAt` (UTC). The bus does not create or read this type; it is available for transports that want a wire envelope.

Example usage:

```csharp
using DotnetEventBus.Advanced;

// The publisher sends the request over your transport and must eventually
// call SendResponse or FailRequest with the same RequestId.
RequestResponseBus? bus = null;
bus = new RequestResponseBus(
    async (eventType, message) =>
    {
        // Example only: deliver the message to a handler in the same process.
        var handler = new StockHandler();
        await handler.ProcessRequestAsync(
            (GetStockRequest)message.Payload,
            message.RequestId!,
            bus!);
    },
    defaultTimeout: TimeSpan.FromSeconds(5));

try
{
    StockResponse? stock = await bus.RequestAsync<GetStockRequest, StockResponse>(
        "stock.get",
        new GetStockRequest("SKU-100"),
        timeout: TimeSpan.FromSeconds(2));

    Console.WriteLine(stock?.Units);
}
catch (TimeoutException)
{
    // No response arrived in time.
}

public sealed record GetStockRequest(string Sku);
public sealed record StockResponse(string Sku, int Units);

public sealed class StockHandler : RequestResponseHandler<GetStockRequest, StockResponse>
{
    public override Task<StockResponse> HandleAsync(GetStockRequest request) =>
        Task.FromResult(new StockResponse(request.Sku, 42));
}
```

Shutdown and diagnostics:

```csharp
// Fail every request that is still waiting when the application stops.
bus.CancelAllRequests("Application shutting down");

int waiting = bus.GetPendingRequestCount();
Dictionary<string, string> states = bus.AuditPendingRequests();
```

## HandlerInvoker

`src/DotnetEventBus/Services/HandlerInvoker.cs` contains the reflection-based invoker that the bus uses to call handlers. It defines the interface `IHandlerInvoker` and its sealed implementation `HandlerInvoker` (namespace `DotnetEventBus.Services`). `AddEventBus()` registers `HandlerInvoker` as a singleton for `IHandlerInvoker`, so most applications never construct it directly. Use it when you need to check or invoke a handler yourself, for example in tests or in custom dispatch code.

Handler lookup is by convention. For an event, the invoker looks for a public, instance `Handle` method with two parameters: the first must be exactly the event's runtime type and the second must be `CancellationToken`. Its return type must be `Task`. For a request, the method must return `Task<TResponse>`. Lookup results are cached per handler type and event type, and the cache is guarded by a lock, so one instance can be shared across threads.

Public API of `IHandlerInvoker` and `HandlerInvoker`:

- `HandlerInvoker(ILogger<HandlerInvoker>? logger = null)` creates the invoker. When a logger is supplied, failures are logged at `Error` level before they are rethrown.
- `Task InvokeAsync(IEventHandler handler, object @event, CancellationToken cancellationToken = default)` calls the matching `Handle` method and awaits it. It throws `ArgumentNullException` for a null `handler` or `@event`. It throws `InvalidOperationException` when the handler has no compatible `Handle` method. Exceptions thrown by the handler are rethrown unwrapped, so you see the original exception rather than `TargetInvocationException`.
- `Task<object?> InvokeRequestAsync(object handler, object request, CancellationToken cancellationToken = default)` calls a `Handle` method that returns `Task<TResponse>` and returns the awaited result as `object?`. It throws `ArgumentNullException` for null arguments and `InvalidOperationException` when no compatible method exists. The method lookup matches the first parameter type and the return type only. The second parameter is passed `cancellationToken`, so a `Handle` method whose second parameter is not `CancellationToken` fails at invocation time.
- `bool CanHandle(IEventHandler handler, Type eventType)` returns `true` when the handler can process `eventType`. For an `IPolymorphicHandler`, it delegates to the handler's own `CanHandle`. For any other handler, it looks for a `Handle` method whose first parameter is exactly `eventType`. Base and derived types are not matched: a handler for `BaseEvent` returns `false` for `typeof(DerivedEvent)`. It throws `ArgumentNullException` for null arguments.
- `IEnumerable<Type> GetSupportedEventTypes(IEventHandler handler)` returns the event types the handler supports. For an `IPolymorphicHandler`, it returns the handler's `GetSupportedEventTypes()` result. For other handlers, it returns the `TEvent` of each `IEventHandler<TEvent>` interface the handler implements. Request and notification interfaces are not included. It throws `ArgumentNullException` for a null `handler`.

Example usage (fire-and-forget event):

```csharp
using DotnetEventBus.Handlers;
using DotnetEventBus.Services;

public sealed record OrderPlacedEvent(string OrderId);

public sealed class OrderPlacedHandler : EventHandlerBase<OrderPlacedEvent>
{
    public override Task Handle(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Order {@event.OrderId} placed");
        return Task.CompletedTask;
    }
}

public sealed class OrderNotifier(IHandlerInvoker invoker)
{
    public async Task NotifyAsync(CancellationToken cancellationToken)
    {
        IEventHandler handler = new OrderPlacedHandler();
        var @event = new OrderPlacedEvent("ORD-1");

        if (invoker.CanHandle(handler, @event.GetType()))
        {
            await invoker.InvokeAsync(handler, @event, cancellationToken);
        }
    }
}
```

Example usage (request/response):

```csharp
using DotnetEventBus.Handlers;
using DotnetEventBus.Services;

public sealed record GetUserQuery(int Id);
public sealed record UserDto(int Id, string Name);

public sealed class GetUserHandler : RequestHandlerBase<GetUserQuery, UserDto>
{
    public override Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserDto(request.Id, "Ada"));
}

public static class UserQueryDemo
{
    public static async Task RunAsync(IHandlerInvoker invoker)
    {
        object? result = await invoker.InvokeRequestAsync(new GetUserHandler(), new GetUserQuery(7));
        var user = (UserDto?)result;

        Console.WriteLine(user?.Name);
    }
}
```

Inspecting supported types:

```csharp
IHandlerInvoker invoker = new HandlerInvoker();
IEventHandler handler = new OrderPlacedHandler();

// Returns typeof(OrderPlacedEvent) for this handler.
IEnumerable<Type> types = invoker.GetSupportedEventTypes(handler);
```

## PerformanceProfiler

`PerformanceProfiler` (namespace `DotnetEventBus.Performance`) measures how long named operations take and keeps the timings in memory. Wrap the code you want to measure with `Profile` or `ProfileAsync`, then read the results with `GetStats`, `GetAllStats`, `GetSummary`, or `GenerateReport`. Timings are wall-clock durations taken from `Stopwatch` and stored as whole milliseconds, so an operation that finishes in under 1 ms is recorded as `0`.

Each execution adds one entry to an in-memory list per operation name, and the lists grow until `Reset()` is called. Use the profiler for development, benchmarking, and diagnostics rather than leaving it on for every request in production. An instance is **not thread-safe**: `Profile`, `ProfileAsync`, and the query methods read and write unsynchronized dictionaries and lists. Use one instance per thread, or wrap every call in your own lock when you share it.

Public API:

- `PerformanceProfiler()` creates an empty profiler and starts the session clock.
- `T Profile<T>(string operationName, Func<T> operation)` runs `operation`, records its elapsed time under `operationName`, and returns its result.
- `void Profile(string operationName, Action operation)` runs `operation` and records its elapsed time.
- `Task<T> ProfileAsync<T>(string operationName, Func<Task<T>> operation)` awaits `operation` and records the time until the task completes.
- `OperationStats? GetStats(string operationName)` returns statistics for one operation, or `null` when nothing has been recorded under that name.
- `IEnumerable<OperationStats> GetAllStats()` returns statistics for every recorded operation. The order is not defined; sort the results yourself if you need a specific order.
- `ProfilingSessionSummary GetSummary()` returns totals for the current session.
- `string GenerateReport()` returns a text report with the session summary followed by each operation, ordered by descending `TotalTimeMs`. The format is meant for people to read and may change, so use the model properties for programmatic access.
- `void Reset()` discards all recorded timings and restarts the session clock.

`Profile` and `ProfileAsync` throw `ArgumentNullException` when `operationName` or `operation` is `null`. If the delegate throws, the exception propagates unchanged and the elapsed time is still recorded. The timing methods do not add any other exception handling.

The returned models expose the following data:

- `OperationStats`: `OperationName`, `ExecutionCount`, and the timings in milliseconds: `TotalTimeMs`, `AverageTimeMs`, `MinTimeMs`, `MaxTimeMs`, `MedianTimeMs`, `P95TimeMs`, and `P99TimeMs`. Average, median, and percentiles are `double` values.
- `ProfilingSessionSummary`: `SessionDuration` (time since construction or the last `Reset()`), `OperationCount` (distinct operation names), `TotalExecutions`, `TotalTimeMs`, `AverageTimeMs` across all executions, and `ThroughputPerSecond`, which is total executions divided by `SessionDuration`. Idle time between calls counts toward the session duration.

Statistics are calculated over every recorded execution. The median is the mean of the two middle values when the count is even. Percentiles use the nearest-rank method: the value at position `ceil(p / 100 × count)` in the sorted timings, so `P95TimeMs` is the smallest recorded value that is at least as large as 95% of the executions.

Example usage:

```csharp
using DotnetEventBus.Models;
using DotnetEventBus.Performance;
using DotnetEventBus.Services;

public sealed record OrderPlacedEvent(string OrderId);

public sealed class OrderPublisher(IEventBus bus)
{
    private readonly PerformanceProfiler _profiler = new();

    public async Task<PublishResult> PublishAsync(string orderId, CancellationToken cancellationToken)
    {
        return await _profiler.ProfileAsync(
            "Publish:OrderPlaced",
            () => bus.PublishAsync(new OrderPlacedEvent(orderId), cancellationToken: cancellationToken));
    }

    public void PrintReport()
    {
        OperationStats? stats = _profiler.GetStats("Publish:OrderPlaced");
        if (stats is not null)
        {
            Console.WriteLine(
                $"{stats.OperationName}: {stats.ExecutionCount} run(s), " +
                $"average {stats.AverageTimeMs:F1} ms, p95 {stats.P95TimeMs:F1} ms");
        }

        Console.WriteLine(_profiler.GenerateReport());
    }
}
```

Synchronous operations and session totals:

```csharp
using DotnetEventBus.Performance;

var profiler = new PerformanceProfiler();

// Synchronous operation with a return value.
int total = profiler.Profile("SumNumbers", () => Enumerable.Range(1, 1_000).Sum());

// Synchronous operation without a return value.
profiler.Profile("WriteLine", () => Console.WriteLine($"Total: {total}"));

// Session-wide totals.
ProfilingSessionSummary summary = profiler.GetSummary();
Console.WriteLine($"{summary.TotalExecutions} executions across {summary.OperationCount} operations");
Console.WriteLine($"Throughput: {summary.ThroughputPerSecond:F2} ops/sec");

// Discard the data and start a new session.
profiler.Reset();
```
