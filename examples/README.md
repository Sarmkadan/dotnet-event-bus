# DotnetEventBus Examples

This directory contains 8 comprehensive example programs demonstrating all major features and patterns of DotnetEventBus.

## Quick Start

Each example is a standalone program. To run an example:

```bash
# Build the example
dotnet build examples/01_BasicPubSub.cs

# Run the example
dotnet run --project examples/01_BasicPubSub.cs
```

Or compile and run directly:

```bash
cd examples
csc /target:exe 01_BasicPubSub.cs
./01_BasicPubSub.exe
```

## Examples Overview

### 1. Basic Pub-Sub (`01_BasicPubSub.cs`)

**What it demonstrates:**
- Basic event publishing and subscription
- Class-based event handlers
- Delegate-based handlers
- Multiple handlers for single event type
- Handler execution flow

**Key Concepts:**
- `PublishAsync()` - Publish an event
- `Subscribe()` - Register async handler
- `SubscribeSync()` - Register sync handler
- Handler ordering by priority

**Learning Path:** Start here for fundamentals

---

### 2. E-Commerce Order Processing (`02_ECommerceOrderProcessing.cs`)

**What it demonstrates:**
- Real-world multi-step workflow
- Handler priorities for execution ordering
- Cascading events (handlers publishing events)
- Complex business logic coordination
- Event-driven architecture pattern

**Key Concepts:**
- Handler priorities (0, 5, 10, 100)
- Sequential handler execution
- Event-driven state transitions
- Domain events in e-commerce

**Scenario:** Order placed → Inventory reserved → Payment processed → Shipment created

**Learning Path:** After basics, understand real-world workflows

---

### 3. Request-Reply Pattern (`03_RequestReplyPattern.cs`)

**What it demonstrates:**
- Synchronous request-response using events
- Request handlers returning responses
- Timeout handling
- Error recovery
- Multiple request-response scenarios

**Key Concepts:**
- `RequestAsync<TRequest, TResponse>()` - Sync request-reply
- Handler responses
- Timeout management
- Query-driven handlers

**Scenarios:**
- User data lookup
- Product availability checking
- Price calculation with discounts

**Learning Path:** Understand synchronous patterns

---

### 4. Dead Letter Queue Handling (`04_DeadLetterQueueHandling.cs`)

**What it demonstrates:**
- Error handling and recovery
- Retry mechanisms and exponential backoff
- Dead letter queue management
- Failed event reprocessing
- Statistics and monitoring

**Key Concepts:**
- Exception handling
- Retry policies
- Dead letter queue operations
- `IDeadLetterService`
- Event recovery strategies

**Operations Shown:**
- Get pending failed events
- Reprocess specific entries
- Permanently delete entries
- View failure statistics

**Learning Path:** Understand production reliability

---

### 5. Performance Metrics & Monitoring (`05_PerformanceMetricsMonitoring.cs`)

**What it demonstrates:**
- Metrics collection
- Performance profiling
- System health checks
- Handler execution metrics
- Real-time monitoring

**Key Concepts:**
- `IMetricsCollector` - System metrics
- `IPerformanceProfiler` - Detailed analysis
- `IHealthCheck` - Health status
- Latency percentiles (P95, P99)
- Throughput measurement

**Metrics Tracked:**
- Total events published
- Success/failure rates
- Average/min/max latency
- Handler-specific metrics
- Memory usage

**Learning Path:** Monitor production systems

---

### 6. Batch Publishing Optimization (`06_BatchPublishingOptimization.cs`)

**What it demonstrates:**
- Efficient batch event publishing
- Performance comparison (individual vs. batch)
- Throughput optimization
- Memory efficiency
- Best practices for high-volume scenarios

**Key Concepts:**
- `IBatchEventPublisher` - Batch operations
- `AddEventAsync()` - Accumulate events
- `FlushAsync()` - Publish batch
- Throughput measurement
- Memory profiling

**Performance Metrics:**
- Individual: ~100 events/sec
- Batched: ~1000+ events/sec (10x improvement)
- Memory efficiency analysis

**Learning Path:** Optimize for scale

---

### 7. Event Filtering (`07_EventFiltering.cs`)

**What it demonstrates:**
- Selective handler execution
- Fluent filter API
- Complex filter composition
- Event properties-based routing
- Multi-criteria filtering

**Key Concepts:**
- `EventFilterBuilder` - Fluent API
- `.Where()` - Add conditions
- `.And()` - Combine filters
- Filter predicates
- Conditional handler execution

**Filter Examples:**
- High-value orders (> $1000)
- Premium customer segment
- Critical alerts only
- Multi-criteria: Enterprise + high value

**Learning Path:** Selective event processing

---

### 8. Subscription Management (`08_SubscriptionManagement.cs`)

**What it demonstrates:**
- Runtime subscription management
- Enable/disable handlers
- Subscription statistics
- Handler monitoring
- Subscription queries

**Key Concepts:**
- `ISubscriptionManager` - Runtime management
- `DisableHandlerAsync()` - Disable handler
- `EnableHandlerAsync()` - Re-enable handler
- `GetStatisticsAsync()` - Handler metrics
- `GetSubscriptionsAsync()` - List subscriptions

**Operations Shown:**
- List all subscriptions
- Get handler statistics
- Disable/enable handlers
- Unsubscribe handlers
- Track invocation metrics

**Learning Path:** Operational management

---

## Feature Coverage Matrix

| Feature | Example 1 | Example 2 | Example 3 | Example 4 | Example 5 | Example 6 | Example 7 | Example 8 |
|---------|-----------|-----------|-----------|-----------|-----------|-----------|-----------|-----------|
| Pub-Sub | ✓ | ✓ | | | | ✓ | ✓ | |
| Request-Reply | | | ✓ | | | | | |
| Handler Priorities | | ✓ | | | | | | ✓ |
| Error Handling | | ✓ | ✓ | ✓ | | | | |
| Dead Letter Queue | | | | ✓ | | | | |
| Batch Publishing | | | | | | ✓ | | |
| Metrics & Monitoring | | | | | ✓ | ✓ | | |
| Event Filtering | | | | | | | ✓ | |
| Subscription Mgmt | | | | | | | | ✓ |

## Running All Examples

To compile and run all examples:

```bash
# Build all examples
cd examples
for i in {01..08}; do
  dotnet build "0${i}_*.cs" 2>/dev/null || dotnet build "${i}_*.cs" 2>/dev/null
done

# Run all examples
for example in *.exe; do
  echo "=== Running $example ==="
  ./$example
  echo
done
```

Or with a script:

```bash
#!/bin/bash
cd examples
for example in 0[1-8]_*.cs; do
  name=${example%.cs}
  echo "Running $name..."
  csc /target:exe "$example" && "./$name.exe"
  echo
done
```

## Expected Output

Each example produces detailed console output showing:

1. **Startup Message** - Identifies the example
2. **Operation Description** - What's happening
3. **Progress Updates** - Event publishing, handler execution
4. **Results** - Success indicators, metrics
5. **Completion** - Final status and summary

## Learning Path Recommendations

### Beginner
1. Start with Example 1 (Basic Pub-Sub)
2. Progress to Example 2 (E-Commerce Order Processing)
3. Try Example 7 (Event Filtering) for selective handling

### Intermediate
4. Study Example 3 (Request-Reply Pattern)
5. Explore Example 4 (Dead Letter Queue)
6. Review Example 8 (Subscription Management)

### Advanced
7. Optimize with Example 6 (Batch Publishing)
8. Monitor with Example 5 (Performance Metrics)

## Extending the Examples

Each example can be extended for learning:

**Add custom events:**
```csharp
public class MyCustomEvent
{
    public string Data { get; set; }
}

eventBus.Subscribe<MyCustomEvent>(
    async (@event, ct) => { /* your logic */ },
    "MyHandler"
);
```

**Add filtering:**
```csharp
var filter = new EventFilterBuilder()
    .Where<MyEvent>(e => e.Value > 100)
    .Build();

eventBus.Subscribe<MyEvent>(handler, "FilteredHandler", filter: filter);
```

**Monitor metrics:**
```csharp
var metrics = metricsCollector.GetSystemMetrics();
Console.WriteLine($"Throughput: {metrics.AverageThroughput} events/sec");
```

## Troubleshooting

**Compilation errors?**
- Ensure DotnetEventBus is in solution
- Check .NET 10.0 SDK is installed
- Verify namespaces match

**Examples fail to run?**
- Check dependencies are restored
- Ensure IEventBus is properly registered
- Review console output for detailed errors

**Want to debug?**
- Add breakpoints in VS Code or Visual Studio
- Use `Console.WriteLine()` for tracing
- Check metrics for performance bottlenecks

## See Also

- **README.md** - Main project documentation
- **docs/getting-started.md** - Getting started guide
- **docs/api-reference.md** - Complete API docs
- **docs/architecture.md** - System design
- **docs/deployment.md** - Production deployment

## Questions?

- Check FAQ: `docs/faq.md`
- Open issue: GitHub Issues
- Discuss: GitHub Discussions
- Contact: [@Sarmkadan](https://t.me/sarmkadan)

---

**Happy learning with DotnetEventBus!** 🚀
