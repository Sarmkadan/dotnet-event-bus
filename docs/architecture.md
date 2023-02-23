# DotnetEventBus Architecture

## Overview

DotnetEventBus is built with a layered, modular architecture designed for performance, testability, and extensibility. This document describes the system design and key components.

## Architecture Layers

```
┌──────────────────────────────────────────┐
│     Application Layer                    │
│  (Events, Handlers, Domain Logic)        │
└───────────────┬──────────────────────────┘
                │
┌───────────────▼──────────────────────────┐
│     Configuration & DI Layer             │
│  (Setup, Options, Builder Pattern)       │
└───────────────┬──────────────────────────┘
                │
┌───────────────▼──────────────────────────┐
│     Pipeline Layer (Middleware)          │
│  (Logging, Error Handling, Rate Limit)   │
└───────────────┬──────────────────────────┘
                │
┌───────────────▼──────────────────────────┐
│     EventBus Core Service                │
│  (Publish, Subscribe, Request-Reply)     │
└───────────────┬──────────────────────────┘
                │
┌───────────────▼──────────────────────────┐
│     Handler Invocation Engine            │
│  (Priority, Concurrency, Timeouts)       │
└───────────────┬──────────────────────────┘
                │
┌───────────────▼──────────────────────────┐
│     Data Access Layer                    │
│  (Repositories, Persistence)             │
└──────────────────────────────────────────┘
```

## Core Components

### 1. IEventBus Service

The primary interface for publishing and subscribing to events.

**Responsibilities:**
- Register event handlers
- Publish events to interested subscribers
- Handle request-reply patterns
- Manage handler lifecycle
- Coordinate with middleware pipeline

**Key Methods:**
- `PublishAsync<TEvent>()` - Publish event
- `RequestAsync<TRequest, TResponse>()` - Request-reply pattern
- `Subscribe<TEvent>()` - Register async handler
- `SubscribeSync<TEvent>()` - Register sync handler
- `UnsubscribeAsync()` - Unregister handler

### 2. Handler Invoker

Responsible for executing registered handlers in the correct order with proper isolation.

**Features:**
- Priority-based execution order
- Concurrent handler execution with configurable limits
- Handler timeout management
- Exception isolation (one handler's failure doesn't affect others)
- Return value collection for request-reply

**Execution Flow:**
1. Sort handlers by priority (descending)
2. Check handler enabled status
3. Apply event filters
4. Execute handler with timeout
5. Capture result or exception
6. Continue with next handler

### 3. Middleware Pipeline

Cross-cutting concerns are handled through a composable middleware pipeline.

**Built-in Middleware:**
- **LoggingMiddleware**: Logs event publish/handle with correlation IDs
- **ErrorHandlingMiddleware**: Captures exceptions, manages retries, routes to DLQ
- **RateLimitingMiddleware**: Prevents event bus overload

**Pipeline Composition:**
```csharp
Request → Logging → ErrorHandling → RateLimiting → EventBus → RateLimiting → ErrorHandling → Logging → Response
```

### 4. Repository Layer

Pluggable data access layer for persistence.

**Interfaces:**
- `IEventMessageRepository` - Store and retrieve events
- `ISubscriptionRepository` - Manage subscriptions
- `IDeadLetterRepository` - Handle failed messages

**Default Implementations:**
- `InMemoryRepository` - In-memory storage (great for testing)
- Custom implementations can use SQL, MongoDB, Redis, etc.

### 5. Support Services

#### DeadLetterService
Manages failed events with retry policies and statistics.

#### SubscriptionManager
Enables/disables handlers and provides subscription statistics.

#### BatchEventPublisher
Aggregates events for batch publishing (performance optimization).

#### MetricsCollector
Tracks system metrics: throughput, latency, success rates.

#### PerformanceProfiler
Detailed performance analysis with percentile reporting.

## Request Lifecycle

### Publishing Flow

```
Application calls eventBus.PublishAsync(event)
    ↓
EventBus resolves event type
    ↓
Pipeline: LoggingMiddleware logs publish start
    ↓
Pipeline: RateLimitingMiddleware checks quota
    ↓
Pipeline: ErrorHandlingMiddleware sets up exception handling
    ↓
EventBus retrieves subscriptions for event type
    ↓
HandlerInvoker sorts handlers by priority
    ↓
For each handler:
  1. Check if enabled
  2. Apply filter (if present)
  3. Create timeout context
  4. Invoke handler with CancellationToken
  5. Capture result/exception
    ↓
Pipeline: ErrorHandlingMiddleware processes exceptions
  - If transient: Add to retry queue
  - If permanent: Send to dead letter queue
    ↓
Pipeline: LoggingMiddleware logs publish completion
    ↓
Return PublishResult with metrics
```

### Request-Reply Flow

```
Application calls eventBus.RequestAsync<TRequest, TResponse>(request)
    ↓
Create temporary response handler with timeout
    ↓
Publish request event
    ↓
Wait for response (with timeout)
    ↓
Remove temporary handler
    ↓
Return response or throw TimeoutException
```

## Key Design Patterns

### 1. Repository Pattern
Data access is abstracted through repository interfaces, allowing different persistence strategies without changing business logic.

### 2. Middleware Pattern
Cross-cutting concerns are handled through a composable middleware pipeline instead of spreading logic throughout the codebase.

### 3. Handler Pattern
Event handling is abstracted through handler interfaces, supporting both class-based and delegate-based handlers.

### 4. Builder Pattern
Fluent configuration API for setting up the event bus with clear, readable syntax.

### 5. Observer Pattern
Core pub-sub mechanism where handlers are observers listening for events.

## Event Filter Architecture

Filters allow selective handler execution based on event properties.

```csharp
Event → Filter Evaluates Predicate
           ↓
         True? → Execute Handler
           ↓
         False → Skip Handler
```

Filters are composed with `AND`/`OR` logic for complex conditions.

## Dead Letter Queue Architecture

Failed events are captured and managed separately.

```
Handler Throws Exception
    ↓
Is Transient? (Network, Timeout, etc.)
    ↓
  Yes → Add to Retry Queue → Exponential Backoff Retry
    ↓
  No → Send to Dead Letter Queue
         ↓
       DLQ Processor monitors for reprocessing
         ↓
       Manual Reprocessing Available
```

## Concurrency Model

### Thread Safety
- EventBus is thread-safe for concurrent publishes
- Subscriptions are protected with locks during registration
- Handler state is isolated per invocation

### Parallelism
- Multiple handlers can execute concurrently (configurable)
- Handlers for different events are never blocked by each other
- Within same event: handlers run concurrently if allowed

### Cancellation
Each handler receives a `CancellationToken` for graceful cancellation support.

## Performance Optimizations

### 1. In-Memory Caching
Frequently accessed data (handlers, subscriptions) cached in memory for fast lookup.

### 2. Batch Publishing
Multiple events can be accumulated and published in a single operation for efficiency.

### 3. Handler Sorting
Handlers pre-sorted by priority once at registration time, not per publish.

### 4. Lazy Initialization
Components initialized only when needed (e.g., DLQ processor only if enabled).

### 5. Concurrent Execution
Handlers execute in parallel for better throughput on multi-core systems.

## Extensibility Points

### 1. Custom Handlers
Implement `IEventHandler<TEvent>` for custom handler logic.

### 2. Custom Repositories
Implement repository interfaces for custom persistence strategies.

### 3. Custom Middleware
Create middleware components by implementing pipeline interface.

### 4. Event Filters
Create complex filters with composition and custom predicates.

### 5. Event Formatters
Implement `IEventFormatter` for custom serialization formats.

## Error Handling Strategy

### Handler-Level
- Handler exceptions are caught and isolated
- One handler's failure doesn't affect others
- Exceptions trigger retry logic or DLQ routing

### System-Level
- Unhandled exceptions in pipeline are logged
- Circuit breaker prevents cascading failures
- Rate limiting prevents overload

### Configuration
- Max retry attempts configurable
- Retry backoff strategy customizable
- Dead letter queue can be disabled if not needed

## Testing Architecture

### Unit Testing
- Handlers tested independently with mocked dependencies
- Event bus behavior tested with in-memory repositories

### Integration Testing
- Full event bus with real repositories
- Handler interaction testing
- End-to-end scenarios

### Performance Testing
- Handler execution timing
- Throughput measurements
- Memory usage profiling

## Deployment Considerations

### In-Process Only
- No external message broker required
- Suitable for monolithic applications
- Low latency, high throughput

### Distributed (Future)
- Can be extended with distributed transports
- Event replication across processes
- Persistent event store

## Security Considerations

- No authentication/authorization built-in (application responsibility)
- Events are published in-process only (no network exposure by default)
- Exception details not exposed to untrusted sources
- Webhook integration includes HMAC-SHA256 signing

## Monitoring & Observability

### Logging
- Structured logging with correlation IDs
- Event-level tracing
- Handler execution tracking

### Metrics
- Event publication counts
- Handler execution times
- Failure rates
- Dead letter queue size

### Health Checks
- Component status verification
- Resource utilization monitoring
- Dead letter queue age tracking

## Version Compatibility

- Backward compatible within major version
- Database schema migrations supported
- Configuration changes handled gracefully

---

For implementation details, see the source code and inline documentation. For deployment guidance, see `deployment.md`.
