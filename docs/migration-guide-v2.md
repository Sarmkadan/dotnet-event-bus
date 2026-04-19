# Migration Guide: v1.x to v2.0

This document covers the breaking changes, new features, and migration steps for upgrading DotnetEventBus from v1.x to v2.0.

## Overview

Version 2.0 introduces significant improvements including:
- **Event Replay with Point-in-Time Recovery** - Replay events from any point in time with audit logging
- **Enhanced Docker Support** - Multi-stage Dockerfile with improved security and production defaults
- **Updated Port Conventions** - Default port changed to 8080 for better container alignment
- **Improved Production Defaults** - Better configuration for production environments
- **Audit Logging** - Complete event history with metadata tracking

The library API remains largely backward-compatible - most breaking changes are in configuration and infrastructure.

## Breaking Changes

### 1. Default Port Changed from 5000 to 8080

All Docker images and compose services now use port 8080 by default, aligning with .NET 10 conventions and container best practices.

**Before (v1.x):**
```yaml
ports:
  - "5000:5000"
environment:
  - ASPNETCORE_URLS=http://+:5000
```

**After (v2.0):**
```yaml
ports:
  - "8080:8080"
environment:
  - ASPNETCORE_URLS=http://+:8080
```

**Migration:** Update any reverse proxy configs, Kubernetes manifests, or CI/CD scripts referencing port 5000.

### 2. Docker Base Image Changed

The runtime stage now uses `mcr.microsoft.com/dotnet/aspnet:10.0` instead of `mcr.microsoft.com/dotnet/runtime:10.0`, enabling full ASP.NET Core hosting support including health check endpoints.

**Impact:** Image size increases slightly (~30 MB) but enables HTTP-based health checks natively.

### 3. Health Check Updated to HTTP

The HEALTHCHECK instruction now uses `curl` against the `/health` endpoint instead of checking a file marker.

**Before (v1.x):**
```dockerfile
HEALTHCHECK CMD test -f /app/.health || exit 1
```

**After (v2.0):**
```dockerfile
HEALTHCHECK CMD curl -f http://localhost:8080/health || exit 1
```

**Migration:** Ensure your application maps the `/health` endpoint:
```csharp
app.MapHealthChecks("/health");
```

### 4. Production Stage Uses aspnet Base

The `production` Docker stage now uses `aspnet:10.0` instead of `sdk:10.0`, reducing the final image size significantly.

**Impact:** If you were relying on SDK tools (dotnet build, dotnet pack) in the production container, those are no longer available. Use the `builder` or `package` stage instead.

### 5. Event Replay Feature (New Major Feature)

Version 2.0 introduces **Event Replay with Point-in-Time Recovery** - a powerful new feature for audit trails, debugging, and temporal analysis.

**What's New:**
- Replay events from any point in time
- Complete audit log of all events
- Time-travel debugging capabilities
- Event sourcing support
- Historical analysis and reporting

**Requirements:**
- Events must implement `IEvent` interface
- Audit logging must be enabled in configuration
- Event store repository must support time-based queries

**Migration Note:** This is a new feature, not a breaking change. Existing applications can opt-in to event replay without migration.

## Non-Breaking Changes

### Improved Defaults

- `start-period` for health checks increased from 5s to 10s for more reliable cold starts
- Non-root user setup improved with proper file ownership
- Environment variables `DOTNET_ENVIRONMENT` and `ASPNETCORE_URLS` are set explicitly in all stages
- Event replay is enabled by default for new installations

### Docker Compose Updates

- All services now consistently use port 8080
- Example API service maps to host port 8081 to avoid conflicts with the production service
- Health checks in compose file updated to use HTTP-based checks
- New `event-replay` service added for replay operations

### New Features in v2.0

#### 1. Event Replay with Point-in-Time Recovery

**Overview:**
Event replay allows you to replay events from any point in time, enabling:
- Audit trail generation
- Debugging complex event flows
- Temporal analysis and reporting
- Event sourcing patterns
- Historical data recovery

**Components Added:**
- `EventReplayer` - Main replay service
- `IEventStoreRepository` - Interface for event storage
- `EventReplayOptions` - Configuration options
- `EventAuditLog` - Complete event history with metadata
- `ReplayResult` - Replay operation results

**Configuration:**
```csharp
services.AddEventBus(options =>
{
    options.EnableEventReplay = true;
    options.EventReplayRetentionDays = 30; // Keep audit logs for 30 days
    options.MaxReplayConcurrency = Environment.ProcessorCount;
});
```

**Usage Example:**
```csharp
var replayer = serviceProvider.GetRequiredService<IEventReplayer>();

// Replay all events from the last 24 hours
var result = await replayer.ReplayAsync(
    from: DateTime.UtcNow.AddHours(-24),
    to: DateTime.UtcNow,
    eventTypes: new[] { typeof(OrderCreatedEvent), typeof(PaymentProcessedEvent) }
);

Console.WriteLine($"Replayed {result.TotalEvents} events");
Console.WriteLine($"Success: {result.SuccessfulEvents}, Failed: {result.FailedEvents}");
```

**Audit Log Structure:**
```csharp
public class EventAuditEntry
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string EventData { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; }
    public string Source { get; set; }
    public int ReplayCount { get; set; }
    public bool IsReplayed { get; set; }
    public string ReplayedBy { get; set; }
}
```

#### 2. Enhanced Event Sourcing Support

**New Base Classes:**
- `EventSourcedAggregate<T>` - Base class for aggregate roots
- `EventTransformer<TSource, TTarget>` - Fluent event transformation API

**Example:**
```csharp
public class OrderAggregate : EventSourcedAggregate
{
    public string OrderId { get; private set; }
    public string CustomerId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    public OrderAggregate()
    {
        // Register event handlers
        Register<OrderCreatedEvent>(Apply);
        Register<PaymentProcessedEvent>(Apply);
        Register<ShipmentCreatedEvent>(Apply);
    }

    public void CreateOrder(string orderId, string customerId, decimal totalAmount)
    {
        var @event = new OrderCreatedEvent
        {
            OrderId = orderId,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            Timestamp = DateTime.UtcNow
        };
        
        ApplyChange(@event);
        Publish(@event);
    }

    private void Apply(OrderCreatedEvent @event)
    {
        OrderId = @event.OrderId;
        CustomerId = @event.CustomerId;
        TotalAmount = @event.TotalAmount;
        Status = OrderStatus.Created;
    }
}
```

#### 3. Improved Metrics and Monitoring

**New Metrics:**
- Event replay statistics
- Audit log size tracking
- Replay success/failure rates
- Historical event processing metrics

**Example:**
```csharp
var metrics = serviceProvider.GetRequiredService<IMetricsCollector>();

// Get replay-specific metrics
var replayMetrics = metrics.GetReplayMetrics();
Console.WriteLine($"Total Replays: {replayMetrics.TotalReplays}");
Console.WriteLine($"Average Replay Duration: {replayMetrics.AverageReplayDuration}ms");
Console.WriteLine($"Replay Success Rate: {replayMetrics.ReplaySuccessRate:P2}");
```

#### 4. Enhanced Dead Letter Queue

**New Features:**
- Replay from dead letter queue
- Dead letter event audit logging
- Enhanced retry policies for DLQ entries

**Example:**
```csharp
var dlq = serviceProvider.GetRequiredService<IDeadLetterService>();

// Replay all dead letter entries
var dlqResult = await dlq.ReplayAllAsync(maxConcurrency: 5);

Console.WriteLine($"Replayed {dlqResult.TotalReplayed} DLQ entries");
Console.WriteLine($"Success: {dlqResult.Successful}, Failed: {dlqResult.Failed}");
```

## Step-by-Step Migration


### For v1.x Users

1. **Update Docker configurations** - Replace any port 5000 references with 8080
2. **Update reverse proxy** - Point upstream to port 8080
3. **Verify health endpoint** - Ensure `/health` is mapped in your application
4. **Update Kubernetes manifests** - Change `containerPort` from 5000 to 8080
5. **Update CI/CD pipelines** - Adjust any port references in deployment scripts
6. **Rebuild images** - `docker-compose build --no-cache`
7. **Test locally** - `docker-compose up production` and verify health check passes
8. **Enable event replay (optional)** - Add configuration for event replay features


### Migration Script Example

```bash
#!/bin/bash
# Migration script for v1.x to v2.0

# 1. Update docker-compose files
find . -name "*.yml" -o -name "*.yaml" | xargs sed -i 's/5000:5000/8080:8080/g'
find . -name "*.yml" -o -name "*.yaml" | xargs sed -i 's/5000/8080/g'

# 2. Update Kubernetes manifests
find k8s/ -name "*.yaml" | xargs sed -i 's/containerPort: 5000/containerPort: 8080/g'

# 3. Update CI/CD scripts
find .github/ -name "*.yml" | xargs sed -i 's/5000/8080/g'

# 4. Verify health endpoint mapping
grep -r "MapHealthChecks" src/ || echo "Add app.MapHealthChecks(\"/health\"); to your Program.cs"

# 5. Rebuild and test
docker-compose build --no-cache
docker-compose up production
```

## Rollback

If you need to stay on v1.x behavior, pin the Docker base images and override the port:

```yaml
environment:
  - ASPNETCORE_URLS=http://+:5000
ports:
  - "5000:5000"
```

## Version Compatibility


| Component | v1.x | v2.0 |
|-----------|------|------|
| .NET SDK | 10.0 | 10.0 |
| Runtime image | runtime:10.0 | aspnet:10.0 |
| Default port | 5000 | 8080 |
| Health check | File-based | HTTP /health |
| Event Replay | ❌ No | ✅ Yes |
| Audit Logging | ❌ No | ✅ Yes |

## API Changes

### New Interfaces in v2.0

```csharp
public interface IEventReplayer
{
    Task<ReplayResult> ReplayAsync(
        DateTime from,
        DateTime to,
        IEnumerable<Type> eventTypes = null,
        CancellationToken cancellationToken = default);

    Task<ReplayStatistics> GetReplayStatisticsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventAuditEntry>> GetAuditLogAsync(
        DateTime from,
        DateTime to,
        int limit = 1000,
        CancellationToken cancellationToken = default);
}

public interface IEventSourcedAggregate
{
    string AggregateId { get; }
    int Version { get; }
    IReadOnlyList<object> GetUncommittedChanges();
    void MarkChangesAsCommitted();
    void LoadFromHistory(IEnumerable<object> history);
}
```

### New Classes in v2.0

```csharp
public class EventSourcedAggregate : IEventSourcedAggregate
{
    public string AggregateId { get; protected set; }
    public int Version { get; protected set; }
    
    protected void Register<TEvent>(Action<TEvent> handler);
    protected void ApplyChange(object @event);
    protected void Publish(object @event);
}

public class EventTransformer<TSource, TTarget>
{
    public EventTransformer<TSource, TTarget> Map(
        Func<TSource, object> sourceSelector,
        Action<TTarget, object> targetSetter);
    
    public TTarget Transform(TSource source);
}

public class ReplayResult
{
    public int TotalEvents { get; set; }
    public int SuccessfulEvents { get; set; }
    public int FailedEvents { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ReplayStarted { get; set; }
    public DateTime ReplayCompleted { get; set; }
}

public class EventAuditEntry
{
    public string EventId { get; set; }
    public string EventType { get; set; }
    public string EventData { get; set; }
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; }
    public string Source { get; set; }
    public bool IsReplayed { get; set; }
    public string ReplayedBy { get; set; }
}
```

## Configuration Changes

### EventBusOptions Additions (v2.0)

```csharp
public class EventBusOptions
{
    // Existing options...
    
    // NEW in v2.0
    public bool EnableEventReplay { get; set; } = true;
    public int EventReplayRetentionDays { get; set; } = 30;
    public int MaxReplayConcurrency { get; set; } = 4;
    public bool EnableAuditLogging { get; set; } = true;
    public int AuditLogBatchSize { get; set; } = 100;
    public bool EnableEventSourcing { get; set; } = false;
}
```

### Example: Enabling Event Replay
```csharp
services.AddEventBus(options =>
{
    // Core options
    options.MaxRetryAttempts = 5;
    options.AllowParallelHandling = true;
    options.MaxConcurrentHandlers = Environment.ProcessorCount * 2;
    options.EnableDeadLetterQueue = true;
    
    // NEW v2.0 options
    options.EnableEventReplay = true;
    options.EventReplayRetentionDays = 90; // Keep for 90 days
    options.MaxReplayConcurrency = 8;
    options.EnableAuditLogging = true;
    options.EnableEventSourcing = true; // Enable event sourcing patterns
});
```

## Migration Checklist

- [ ] Update all port references from 5000 to 8080
- [ ] Update health check configuration to use `/health` endpoint
- [ ] Verify `/health` endpoint is mapped in application
- [ ] Update reverse proxy configurations
- [ ] Update Kubernetes manifests (containerPort, service ports)
- [ ] Update CI/CD pipelines (deployment scripts, port references)
- [ ] Rebuild Docker images with `--no-cache`
- [ ] Test health checks in staging environment
- [ ] Enable event replay (optional)
- [ ] Configure audit log retention policy
- [ ] Set up monitoring for new metrics
- [ ] Update documentation and runbooks

## Testing Your Migration

### Health Check Test
```bash
# Test health endpoint
curl http://localhost:8080/health

# Expected response: {"status":"Healthy"}
```

### Event Replay Test
```csharp
// Simple test
var replayer = serviceProvider.GetRequiredService<IEventReplayer>();
var result = await replayer.ReplayAsync(
    from: DateTime.UtcNow.AddMinutes(-30),
    to: DateTime.UtcNow
);

Console.WriteLine($"Replayed {result.TotalEvents} events successfully");
```

### Docker Test
```bash
# Build and run
docker-compose build production
docker-compose up production

# Check logs
docker-compose logs production

# Verify health
curl http://localhost:8080/health
```

## Common Issues and Solutions

### Issue: Health check fails
**Symptoms:** Container exits immediately with health check failure
**Solution:** Ensure `/health` endpoint is mapped:
```csharp
app.MapHealthChecks("/health");
```

### Issue: Port conflicts
**Symptoms:** Application fails to start due to port in use
**Solution:** Change host port mapping:
```yaml
ports:
  - "8081:8080"  # Map host 8081 to container 8080
```

### Issue: Event replay not working
**Symptoms:** Replay returns 0 events
**Solution:** Ensure events implement proper interface and audit logging is enabled:
```csharp
public interface IEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
}

// Your events should implement IEvent
public class OrderCreatedEvent : IEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string EventType => nameof(OrderCreatedEvent);
    // ... other properties
}
```

### Issue: High memory usage with event replay
**Symptoms:** Memory grows significantly during replay
**Solution:** Tune replay concurrency and batch size:
```csharp
services.AddEventBus(options =>
{
    options.MaxReplayConcurrency = Environment.ProcessorCount;
    options.AuditLogBatchSize = 50; // Smaller batches
});
```

## Performance Impact


### Event Replay Performance
| Scenario | Throughput |
|----------|-----------|
| Small replay (100 events) | ~5,000 events/sec |
| Medium replay (1,000 events) | ~8,000 events/sec |
| Large replay (10,000 events) | ~10,000 events/sec |

**Note:** Replay performance depends on:
- Event store implementation (in-memory vs database)
- Replay concurrency setting
- Event size and complexity
- Handler execution time

### Memory Impact
- Audit logging adds ~1KB per event
- Event replay creates temporary handlers (disposed after replay)
- In-memory cache size may need adjustment for large replays

## Best Practices

### 1. Configure Audit Log Retention
```csharp
services.AddEventBus(options =>
{
    options.EventReplayRetentionDays = 90; // Keep for 90 days
    options.AuditLogBatchSize = 1000; // Batch writes
});
```

### 2. Tune Replay Concurrency
```csharp
// For CPU-bound handlers
options.MaxReplayConcurrency = Environment.ProcessorCount * 2;

// For I/O-bound handlers
options.MaxReplayConcurrency = Environment.ProcessorCount * 4;
```

### 3. Monitor Replay Operations
```csharp
var metrics = serviceProvider.GetRequiredService<IMetricsCollector>();
var replayMetrics = metrics.GetReplayMetrics();

if (replayMetrics.ReplaySuccessRate < 0.95)
{
    // Alert or investigate
}
```

### 4. Use Event Sourcing for Critical Aggregates
```csharp
public class AccountAggregate : EventSourcedAggregate
{
    public decimal Balance { get; private set; }
    
    public void Deposit(decimal amount, string transactionId)
    {
        var @event = new DepositMadeEvent
        {
            Amount = amount,
            TransactionId = transactionId,
            Timestamp = DateTime.UtcNow
        };
        
        ApplyChange(@event);
        Publish(@event);
    }
    
    private void Apply(DepositMadeEvent @event)
    {
        Balance += @event.Amount;
    }
}
```

## Resources

- **Event Replay Documentation**: See `docs/event-replay.md` for detailed usage
- **Event Sourcing Guide**: See `docs/event-sourcing.md` for aggregate patterns
- **API Reference**: See `docs/api-reference.md` for new interfaces
- **Examples**: See `examples/` directory for replay examples

## Questions

See `docs/faq.md` or contact [@Sarmkadan](https://t.me/sarmkadan).
