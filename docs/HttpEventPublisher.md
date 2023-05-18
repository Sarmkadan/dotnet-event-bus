# HttpEventPublisher

`HttpEventPublisher` is a concrete implementation for publishing events over HTTP. It serializes event payloads and delivers them to a configured endpoint, with built-in retry logic and correlation capabilities. The class exposes configurable parameters for controlling retry behavior, timeouts, and correlation header naming, and returns a structured result indicating success or failure along with the HTTP status code and any error details.

## API

### HttpEventPublisher

```csharp
public HttpEventPublisher(HttpEventPublisherOptions options)
```

Constructs a new instance configured with the supplied options. The options specify the target endpoint, serialization settings, authentication, and default values for retries, delays, and timeouts. The constructor validates that required options are present and throws an `ArgumentNullException` if `options` is `null`, or an `ArgumentException` if mandatory fields such as the endpoint URI are missing or invalid.

### PublishAsync

```csharp
public async Task<HttpPublishResult> PublishAsync<TEvent>(TEvent event, CancellationToken cancellationToken = default)
```

Serializes the provided event object and sends it as an HTTP request to the configured endpoint. If the request fails due to a transient HTTP status code or a network-level exception, the method retries up to `MaxRetries` times, waiting `RetryDelayMs` milliseconds between attempts. The overall operation is bounded by `Timeout`. If a `CorrelationIdHeaderName` is set, the publisher adds a correlation identifier header to the outgoing request. The returned `HttpPublishResult` indicates whether the publish ultimately succeeded, the final HTTP status code, and any error message captured during failures. Throws `OperationCanceledException` if the cancellation token is signaled. Throws `InvalidOperationException` if the publisher has been disposed.

### MaxRetries

```csharp
public int MaxRetries { get; set; }
```

Gets or sets the maximum number of retry attempts for failed publish operations. A value of zero means no retries are performed. The default is taken from the options provided at construction. Setting a negative value causes an `ArgumentOutOfRangeException` on the next publish attempt.

### RetryDelayMs

```csharp
public int RetryDelayMs { get; set; }
```

Gets or sets the delay in milliseconds between retry attempts. This delay is applied before each retry except the first attempt. The default is taken from the construction options. Values less than zero are clamped to zero internally.

### Timeout

```csharp
public TimeSpan Timeout { get; set; }
```

Gets or sets the overall timeout for a publish operation, including all retry attempts. If the cumulative time spent exceeds this value, the operation is aborted and the result indicates failure. The default is taken from the construction options. Setting a negative or zero timeout causes an `ArgumentOutOfRangeException` on the next publish attempt.

### CorrelationIdHeaderName

```csharp
public string? CorrelationIdHeaderName { get; set; }
```

Gets or sets the name of the HTTP header used to convey a correlation identifier. When set to a non-null, non-empty string, each outgoing request includes this header with a newly generated GUID value, enabling end-to-end tracing across services. When `null` or empty, no correlation header is added. The default is taken from the construction options.

### Success

```csharp
public bool Success { get; }
```

Part of `HttpPublishResult`. Indicates whether the event was published successfully. `true` if the HTTP response had a success status code (2xx range) and no exception occurred; `false` otherwise.

### StatusCode

```csharp
public int StatusCode { get; }
```

Part of `HttpPublishResult`. Contains the HTTP status code from the final response. If the operation failed before receiving any response (e.g., due to a timeout or network error), this is set to 0.

### ErrorMessage

```csharp
public string? ErrorMessage { get; }
```

Part of `HttpPublishResult`. Provides a human-readable error description when `Success` is `false`. This may contain the exception message, a summary of retry exhaustion, or details about a non-success status code. Returns `null` when the publish succeeded.

### HttpPublishResult

```csharp
public HttpPublishResult(bool success, int statusCode, string? errorMessage)
```

Constructs an immutable result instance representing the outcome of a publish attempt. The constructor is typically called internally by `HttpEventPublisher` and is exposed for testing or custom result creation scenarios.

## Usage

### Basic Publish with Default Settings

```csharp
var options = new HttpEventPublisherOptions
{
    Endpoint = new Uri("https://api.example.com/events"),
    MaxRetries = 3,
    RetryDelayMs = 500,
    Timeout = TimeSpan.FromSeconds(10)
};

var publisher = new HttpEventPublisher(options);

var orderEvent = new OrderCreatedEvent { OrderId = 12345, Amount = 99.95m };
HttpPublishResult result = await publisher.PublishAsync(orderEvent);

if (result.Success)
{
    Console.WriteLine($"Event published successfully. Status: {result.StatusCode}");
}
else
{
    Console.WriteLine($"Publish failed: {result.ErrorMessage}");
}
```

### Publishing with Correlation and Custom Retry Tuning

```csharp
var options = new HttpEventPublisherOptions
{
    Endpoint = new Uri("https://orders.service.internal/events"),
    MaxRetries = 5,
    RetryDelayMs = 1000,
    Timeout = TimeSpan.FromSeconds(30),
    CorrelationIdHeaderName = "X-Correlation-Id"
};

var publisher = new HttpEventPublisher(options);

// Override retry settings at runtime for a specific critical event
publisher.MaxRetries = 10;
publisher.RetryDelayMs = 2000;

var shipmentEvent = new ShipmentDispatchedEvent { TrackingNumber = "TRK-987654" };

try
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
    HttpPublishResult result = await publisher.PublishAsync(shipmentEvent, cts.Token);

    if (!result.Success)
    {
        // Log failure details and potentially store event for later replay
        logger.LogError("Publish failed with status {StatusCode}: {Error}",
            result.StatusCode, result.ErrorMessage);
    }
}
catch (OperationCanceledException)
{
    logger.LogWarning("Publish operation was cancelled due to timeout or manual cancellation");
}
```

## Notes

- **Retry semantics**: Retries are performed only for transient failures—typically HTTP status codes 408, 429, 5xx, and `HttpRequestException` or `TaskCanceledException` caused by network issues. Non-transient status codes (e.g., 400, 401, 403) are not retried and immediately return a failure result.
- **Timeout enforcement**: The `Timeout` property governs the total elapsed wall-clock time across all attempts. If a single attempt exceeds the remaining time budget, it is cancelled and no further retries occur.
- **Thread safety**: Instance members `MaxRetries`, `RetryDelayMs`, `Timeout`, and `CorrelationIdHeaderName` are not synchronized. Concurrent calls to `PublishAsync` while mutating these properties may result in inconsistent behavior. Configure properties before initiating publishes, or synchronize externally if runtime changes are required during concurrent usage.
- **Disposal**: The publisher owns an internal `HttpClient` instance. Call `Dispose` (via `IDisposable` if implemented on the base or via `IAsyncDisposable`) when the publisher is no longer needed to free underlying connections. Attempting to call `PublishAsync` after disposal throws an `InvalidOperationException`.
- **Correlation ID generation**: When `CorrelationIdHeaderName` is set, a new GUID is generated per publish attempt, not per retry cycle. All retries for the same event carry the same correlation ID, enabling grouping of related attempts in downstream logs.
- **Result immutability**: `HttpPublishResult` is a value-like immutable type. Its properties reflect the state at the moment of construction and do not change.
