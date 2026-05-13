# Changelog

All notable changes to the DotnetEventBus project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-16

### Added
- Comprehensive documentation: architecture guide, API reference, FAQ, deployment guide, getting-started walkthrough
- Dockerfile and docker-compose.yml for containerised development and production deployment
- CI/CD workflow with GitHub Actions: build, test, CodeQL analysis, and NuGet publish pipelines
- Makefile targets: `build`, `test`, `release`, `clean`, `lint`
- .editorconfig for consistent code style across editors
- 8 complete runnable example programs covering all major features
- REST API controller for event operations and monitoring endpoints
- Health check framework with configurable thresholds
- CLI command interface (`publish`, `subscribe`, `stats`, `query` subcommands)
- `IPerformanceProfiler` with p50/p95/p99 percentile reporting
- `IMetricsCollector` for throughput, latency, and success-rate tracking
- `IEventCache` with LRU eviction and TTL expiry
- `DeadLetterProcessor` hosted background worker for automatic retry
- `WebhookHandler` with HMAC-SHA256 request signing
- `HttpEventPublisher` for outbound HTTP event delivery
- Multiple output formatters: JSON, CSV, XML with extensible `IEventFormatter`
- 30+ utility extension methods across collections, strings, reflection, and types

### Changed
- `EventBusOptions` defaults tuned for production (parallel on, DLQ on, 30 s handler timeout)
- README expanded to full reference documentation with working code samples
- Error messages now include handler name, event type, and retry attempt count

### Fixed
- Race condition in `SubscriptionRepository` under high-concurrency unsubscribe
- `BatchEventPublisher.FlushAsync` could silently drop the final batch on cancellation

## [0.8.0] - 2025-05-05

### Added
- Middleware pipeline with composable, ordered `IEventMiddleware` execution
- `EventBusLoggingMiddleware`: structured logging with correlation IDs
- `ErrorHandlingMiddleware`: per-handler exception capture, configurable retry
- `RateLimitingMiddleware`: token-bucket limiter with per-event-type overrides
- `PipelineBuilder` and `PipelineBuilderExtensions` for fluent middleware registration
- `SagaOrchestrator` with step sequencing and automatic compensation on failure
- `EventSourcedAggregate` base class for domain aggregate implementations
- `EventTransformer<TSource, TTarget>` fluent mapping builder
- `EventFilterBuilder` with predicate composition (`.Where`, `.And`, `.Or`)
- `PredicateFilteredHandler` and `PredicateSubscriptionBuilder` for inline filters
- `CircuitBreaker` with configurable failure threshold and half-open probe interval
- `RetryPolicy` with exponential backoff and full jitter

### Changed
- `HandlerInvoker` now supports priority-based ordering and configurable concurrency limits
- `EventBusBuilder` extended with `WithMiddleware`, `WithRetryPolicy`, `WithCircuitBreaker`

## [0.5.0] - 2025-03-31

### Added
- Dead letter queue: `IDeadLetterService`, `DeadLetterRepository`, `DeadLetterEntry`
- `DeadLetterStatistics` with per-event-type failure counts and reprocess tracking
- Retry policies with exponential backoff on handler failure
- Handler priority ordering via `Subscription.Priority`
- Concurrent handler execution with `MaxConcurrentHandlers` ceiling
- Handler timeout enforcement via `CancellationTokenSource` per invocation
- `IBatchEventPublisher` for buffered, high-throughput event ingestion
- `RequestResponsePattern` for typed request-reply over the event bus
- `EventEnvelope` wrapping events with metadata (correlation ID, timestamp, source)
- `PublishResult` with `HandlersInvoked`, `HandlersFailed`, and `Duration` fields
- `IEventBusHealthCheck` reporting queue depth, DLQ size, and handler error rates

### Changed
- `EventBus.PublishAsync` returns `PublishResult` instead of `void`
- `IEventHandler<T>` base updated to `EventHandlerBase<T>` with lifecycle hooks

### Fixed
- `SubscriptionManager` leaked `Subscription` entries after `UnsubscribeAsync`

## [0.2.0] - 2025-02-24

### Added
- Core in-process pub/sub with `EventBus`, `IEventBus`, and `SubscriptionManager`
- `IEventHandler<T>` interface and `HandlerBase<T>` abstract base class
- Delegate-based subscriptions (`Subscribe<T>(Func<T, CancellationToken, Task>)`)
- Synchronous handler variant (`SubscribeSync<T>(Action<T>)`)
- Polymorphic handler discovery via `ReflectionHelper`
- Repository pattern: `IRepository<T>`, `InMemoryRepository<T>`, `EventMessageRepository`, `SubscriptionRepository`
- `ServiceCollectionExtensions.AddEventBus(...)` for DI registration
- `EventBusBuilder` fluent configuration API
- `EventBusOptions` with sane defaults
- `EventBusException` hierarchy for typed error handling
- Structured logging via `ILogger<T>` throughout the core pipeline
- `EventRoutingConfiguration` for conditional event routing rules
- Comprehensive xUnit + FluentAssertions + Moq test suite

### Changed
- Moved event models into dedicated `Models/` namespace
- Renamed `EventDispatcher` → `EventBus` for clarity

## [0.1.0] - 2025-02-03

### Initial Release
- Project scaffolding: solution structure, `src/` and `tests/` layout
- Basic `IEventBus` interface definition
- `EventMessage` and `Subscription` models
- `IEventHandler<T>` interface
- `IRepository<T>` interface with stub in-memory implementation
- Initial xUnit test project wired up
- MIT License
- .gitignore and .editorconfig stubs

---

### Version Compatibility

| Version | .NET | Status     |
|---------|------|------------|
| 1.0.0   | 10.0 | Active     |
| 0.8.0   | 10.0 | Supported  |
| 0.5.0   | 10.0 | Maintained |
| 0.2.0   | 10.0 | Outdated   |
| 0.1.0   | 10.0 | Outdated   |

### Breaking Changes

**0.5.0:**
- `EventBus.PublishAsync` now returns `PublishResult` (was `void`)
- Handler base class renamed from `HandlerBase<T>` to `EventHandlerBase<T>`

**1.0.0:**
- No breaking changes (fully backward compatible with 0.8.x)

### Credits

- **Author**: Vladyslav Zaiets (@Sarmkadan)
- **Portfolio**: https://sarmkadan.com
