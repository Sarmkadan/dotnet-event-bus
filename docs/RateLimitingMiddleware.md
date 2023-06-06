# RateLimitingMiddleware

A middleware component for the `dotnet-event-bus` library that enforces rate limits on event processing to prevent system overload or abuse. It integrates with the middleware pipeline to track and restrict the frequency of incoming events based on configurable bucket policies.

## API

### `public RateLimitingMiddleware`
Initializes a new instance of the rate-limiting middleware with default or configured rate-limiting parameters. The middleware must be registered in the event bus pipeline to take effect.

### `public EventBusMiddleware Create`
Creates a middleware instance configured for the event bus pipeline. This method is typically invoked during middleware registration to integrate the rate limiter into the processing flow.

### `public RateLimitBucket`
Gets the rate-limiting bucket associated with this middleware instance. The bucket defines the constraints (e.g., request count, time window) under which events are allowed or rejected.

### `public bool IsAllowed`
Determines whether the next event in the pipeline should be processed based on the current rate-limiting state. Returns `true` if the event is permitted; otherwise, returns `false`.

### `public void RecordRequest`
Increments the internal counter for processed events. This method is called automatically by the middleware when an event passes the rate limit check, and should not be invoked manually under normal operation.

### `public RateLimitExceededException(string message) : base(message)`
Constructs a new exception instance with a descriptive message indicating that a rate limit has been exceeded. This exception is thrown by the middleware when `IsAllowed` returns `false` and the caller should handle or propagate it accordingly.
- **Parameters**:
  - `message` (string): A human-readable explanation of the rate limit violation.

## Usage
