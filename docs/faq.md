# Frequently Asked Questions (FAQ)

Common questions and troubleshooting for DotnetEventBus.

## Installation & Setup

**Q: What .NET versions are supported?**
A: DotnetEventBus requires .NET 10.0 or later. Earlier versions (.NET 6, 7, 8, 9) are not supported.

**Q: Can I use DotnetEventBus with ASP.NET Core?**
A: Yes! DotnetEventBus works seamlessly with ASP.NET Core. Add it to your service collection:
```csharp
services.AddEventBus();
var eventBus = sp.GetRequiredService<IEventBus>();
```

**Q: Is DotnetEventBus available on NuGet?**
A: Yes. Install via: `dotnet add package DotnetEventBus`

**Q: Can I self-host or use a custom NuGet feed?**
A: Yes. You can build from source and reference locally, or host on a private NuGet server.

## Usage & Patterns

**Q: How do I publish an event?**
A: Use the `PublishAsync` method:
```csharp
var result = await eventBus.PublishAsync(new MyEvent { /* ... */ });
```

**Q: How do I subscribe to events?**
A: Use the `Subscribe` method:
```csharp
eventBus.Subscribe<MyEvent>(
    async (@event, ct) => { /* handle */ },
    handlerName: "MyHandler"
);
```

**Q: Can I have multiple handlers for the same event?**
A: Yes! Register as many handlers as needed. They'll execute based on priority:
```csharp
eventBus.Subscribe<OrderEvent>(handler1, "Handler1", priority: 10);
eventBus.Subscribe<OrderEvent>(handler2, "Handler2", priority: 5);
// Handler1 executes first, then Handler2
```

**Q: What's the difference between `Subscribe` and `SubscribeSync`?**
A: `Subscribe` is async (`Task`), `SubscribeSync` is synchronous. Use sync only when necessary (no I/O).

**Q: How do I implement request-reply pattern?**
A: Use `RequestAsync`:
```csharp
// Handler publishes response
eventBus.Subscribe<Request>(
    async (req, ct) => await PublishResponseAsync(response),
    "ResponseHandler"
);

// Client waits for response
var response = await eventBus.RequestAsync<Request, Response>(request);
```

**Q: Can handlers be priority-ordered?**
A: Yes! Higher priority executes first:
```csharp
eventBus.Subscribe<Event>(handler, "Critical", priority: 100);
eventBus.Subscribe<Event>(handler, "Normal", priority: 0);
eventBus.Subscribe<Event>(handler, "Low", priority: -50);
```

**Q: How do I filter events?**
A: Use event filters:
```csharp
var filter = new EventFilterBuilder()
    .Where<MyEvent>(e => e.Priority > 5)
    .Build();

eventBus.Subscribe<MyEvent>(handler, "HighPriorityHandler", filter: filter);
```

## Performance & Scaling

**Q: How many events per second can DotnetEventBus handle?**
A: Depends on handler complexity and system resources. Typical: 1,000-10,000 events/sec. Test your workload.

**Q: Should I use batch publishing or individual publishes?**
A: Use `BatchEventPublisher` for better throughput when publishing many events:
```csharp
var batch = sp.GetRequiredService<IBatchEventPublisher>();
for (int i = 0; i < 1000; i++)
    await batch.AddEventAsync(new Event { Id = i });
await batch.FlushAsync();
```

**Q: Can handlers run in parallel?**
A: Yes! Set `AllowParallelHandling = true`. Control concurrency with `MaxConcurrentHandlers`.

**Q: What's the best handler timeout value?**
A: Use `TimeSpan.FromSeconds(30)` as a default. Adjust based on your handler complexity.

**Q: Should I use in-memory or database repository?**
A: Use in-memory for testing/development. Use database (PostgreSQL, SQL Server) for production.

## Reliability & Error Handling

**Q: What happens if a handler throws an exception?**
A: The exception is caught. If transient, it's retried. If persistent, it goes to the dead letter queue.

**Q: How do I know if a handler failed?**
A: Check the `PublishResult`:
```csharp
var result = await eventBus.PublishAsync(evt);
if (result.HandlersFailed > 0)
    Console.WriteLine($"Failed: {result.HandlersFailed}");
```

**Q: How do I reprocess failed events?**
A: Use the dead letter service:
```csharp
var dlq = sp.GetRequiredService<IDeadLetterService>();
var pending = await dlq.GetPendingEntriesAsync();
foreach (var entry in pending)
    await dlq.ReprocessEntryAsync(entry.Id);
```

**Q: How many times will a handler be retried?**
A: By default, 3 times. Configure with `MaxRetryAttempts`:
```csharp
options.MaxRetryAttempts = 5;
```

**Q: What's exponential backoff?**
A: Retry delays increase exponentially:
- Attempt 1: 100ms
- Attempt 2: 200ms (100 × 2)
- Attempt 3: 400ms (200 × 2)

Configure with `RetryDelayMultiplier` and `InitialRetryDelayMs`.

**Q: Can I disable the dead letter queue?**
A: Yes, but not recommended for production:
```csharp
options.EnableDeadLetterQueue = false;
```

**Q: What's the circuit breaker pattern?**
A: Prevents cascading failures by stopping requests when error rate is high. Automatically re-enables when recovered.

## Testing

**Q: How do I unit test handlers?**
A: Mock dependencies and invoke handler directly:
```csharp
var mockService = new Mock<IService>();
var handler = new MyHandler(mockService.Object);
await handler.Handle(new MyEvent(), CancellationToken.None);
mockService.Verify(x => x.DoSomething());
```

**Q: How do I test event publishing?**
A: Use in-memory repositories:
```csharp
services.AddEventBus(
    new InMemoryRepository(),
    new InMemoryRepository(),
    new InMemoryRepository()
);
```

**Q: Can I mock the event bus?**
A: Yes, create a mock:
```csharp
var mockBus = new Mock<IEventBus>();
mockBus.Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new PublishResult { HandlersInvoked = 1 });
```

## Monitoring & Debugging

**Q: How do I see what events are being published?**
A: Enable detailed logging:
```csharp
options.EnableDetailedLogging = true;

// Also enable ILogger
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
```

**Q: How do I get performance metrics?**
A: Use the metrics collector:
```csharp
var metrics = sp.GetRequiredService<IMetricsCollector>();
var stats = metrics.GetSystemMetrics();
Console.WriteLine($"Throughput: {stats.AverageThroughput} events/sec");
```

**Q: Can I see handler execution times?**
A: Yes, from PublishResult:
```csharp
var result = await eventBus.PublishAsync(evt);
foreach (var handler in result.HandlerResults)
    Console.WriteLine($"{handler.HandlerName}: {handler.Duration.TotalMilliseconds}ms");
```

**Q: How do I profile performance?**
A: Use the performance profiler:
```csharp
var profiler = sp.GetRequiredService<IPerformanceProfiler>();
var report = profiler.GenerateReport();
Console.WriteLine(report);
```

**Q: Where are logs written?**
A: Depends on your logger provider. Common options:
- Console: `builder.AddConsole()`
- File: `builder.AddFile("logs/eventbus.log")`
- Structured: `builder.AddJsonConsole()`

## Deployment & Operations

**Q: Can I use DotnetEventBus in Docker?**
A: Yes! Use the provided Dockerfile:
```bash
docker build -t eventbus:latest .
docker run eventbus:latest
```

**Q: Can I deploy to Kubernetes?**
A: Yes! See `docs/deployment.md` for a Kubernetes manifest example.

**Q: How do I do a graceful shutdown?**
A: Use `IHostApplicationLifetime`:
```csharp
app.Services.GetRequiredService<IHostApplicationLifetime>()
    .ApplicationStopping.Register(() =>
{
    // Flush pending events, cleanup resources
});
```

**Q: Should I use SQL Server or PostgreSQL?**
A: Both work. PostgreSQL is recommended for its robust JSON support. SQL Server is fine if you're in Microsoft ecosystem.

**Q: How do I backup events?**
A: Use database backup tools:
```bash
# PostgreSQL
pg_dump eventbus > backup.sql

# SQL Server
sqlcmd -S server -d eventbus -Q "BACKUP DATABASE eventbus TO DISK='/backup/eventbus.bak'"
```

**Q: What's a good monitoring strategy?**
A: Monitor:
- Event throughput (events/sec)
- Handler latency (p50, p95, p99)
- Dead letter queue size
- Handler success rate
- Memory/CPU usage

**Q: How do I handle scale-out (multiple instances)?**
A: Use a shared database:
```csharp
// All instances point to same DB
services.AddEventBus(
    new PostgresEventMessageRepository(sharedConnectionString),
    // ...
);
```

## Advanced Topics

**Q: Can I use event sourcing with DotnetEventBus?**
A: Yes! Use `EventSourcedAggregate` as a base class for your domain models.

**Q: Can I implement CQRS?**
A: Yes! Use separate event bus instances for commands and queries, or use the same bus with different handlers.

**Q: Can I use sagas for distributed transactions?**
A: Yes! Use `SagaOrchestrator` to coordinate multi-step processes with rollback.

**Q: Can I integrate with message brokers (RabbitMQ, etc.)?**
A: Not built-in, but you can extend by creating custom middleware or repositories.

**Q: Can I use DotnetEventBus for real-time notifications?**
A: Yes! Publish events and have SignalR handlers broadcast to clients.

**Q: Is DotnetEventBus GDPR compliant?**
A: No built-in data retention policies. Implement at application level if needed.

## Support & Contributions

**Q: Where do I report bugs?**
A: Open an issue on [GitHub](https://github.com/Sarmkadan/dotnet-event-bus/issues).

**Q: Can I contribute?**
A: Yes! Contributions welcome. See CONTRIBUTING.md for guidelines.

**Q: Is there a community forum?**
A: Yes, [GitHub Discussions](https://github.com/Sarmkadan/dotnet-event-bus/discussions).

**Q: How do I get support?**
A: 
- Check the FAQ and docs
- Search existing GitHub issues
- Open a new issue if not found
- Contact [@Sarmkadan](https://t.me/sarmkadan) on Telegram

**Q: Is DotnetEventBus production-ready?**
A: Yes! Version 1.0+ is production-ready with comprehensive testing, error handling, and observability.

---

Can't find your answer? Open an issue or start a discussion on [GitHub](https://github.com/Sarmkadan/dotnet-event-bus).
