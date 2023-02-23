# Deployment Guide

Production deployment strategies and best practices for DotnetEventBus.

## Environment Setup

### Development Environment

```bash
# Clone and setup
git clone https://github.com/Sarmkadan/dotnet-event-bus.git
cd dotnet-event-bus

# Install dependencies
dotnet restore

# Build locally
make build

# Run tests
make test

# Run specific example
cd examples/DotnetEventBus.Examples.ECommerce
dotnet run
```

### Docker Deployment

```bash
# Build development image
docker-compose build dev

# Run development container
docker-compose up dev

# Run tests in container
docker-compose up test

# Create production image
docker build --target production -t dotnet-event-bus:1.2.0 .

# Run production container
docker run -d \
  --name eventbus-prod \
  -e DOTNET_ENVIRONMENT=Production \
  dotnet-event-bus:1.2.0
```

## Deployment Strategies

### Strategy 1: Single Server

Suitable for small applications with modest event volumes.

```
┌──────────────────────┐
│   Single Server      │
│  ┌────────────────┐  │
│  │  .NET App      │  │
│  │  EventBus      │  │
│  │  Handlers      │  │
│  └────────────────┘  │
│  ┌────────────────┐  │
│  │ InMemory DB    │  │
│  └────────────────┘  │
└──────────────────────┘
```

**Configuration:**
```csharp
services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 3;
    options.AllowParallelHandling = true;
    options.MaxConcurrentHandlers = 4;
    options.EnableDeadLetterQueue = true;
});
```

**Limitations:**
- No persistence across restarts
- No horizontal scaling
- Single point of failure

### Strategy 2: Server + Database

Add persistent storage for event history and subscriptions.

```
┌──────────────────────┐
│   Application        │
│   Server             │
│  ┌────────────────┐  │
│  │  .NET App      │  │
│  │  EventBus      │  │
│  └────────────────┘  │
└──────────────────────┘
         │
         ▼ SQL Queries
┌──────────────────────┐
│   PostgreSQL/        │
│   SQL Server         │
└──────────────────────┘
```

**Configuration:**
```csharp
services.AddEventBus(
    new SqlEventMessageRepository(_connectionString),
    new SqlSubscriptionRepository(_connectionString),
    new SqlDeadLetterRepository(_connectionString),
    options =>
    {
        options.EnableDeadLetterQueue = true;
    }
);
```

**Connection String (PostgreSQL):**
```
Server=db.example.com;Database=eventbus;User=eventbus;Password=secure-password;
```

### Strategy 3: Load Balanced

Multiple application servers behind a load balancer for high availability.

```
         ┌─────────────────┐
         │  Load Balancer  │
         └────────┬────────┘
      ┌──────────┼──────────┐
      ▼          ▼          ▼
   ┌─────┐   ┌─────┐   ┌─────┐
   │App 1│   │App 2│   │App 3│
   └──┬──┘   └──┬──┘   └──┬──┘
      └─────────┼─────────┘
               ▼
        ┌─────────────────┐
        │   Shared DB     │
        │  PostgreSQL     │
        └─────────────────┘
```

**Nginx Configuration:**
```nginx
upstream eventbus_backend {
    server app1.example.com:5000;
    server app2.example.com:5000;
    server app3.example.com:5000;
}

server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass http://eventbus_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

### Strategy 4: Kubernetes Deployment

Enterprise deployment with auto-scaling and self-healing.

**Dockerfile:**
Already provided in repository.

**Kubernetes Manifest (eventbus-deployment.yaml):**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dotnet-event-bus
  namespace: production
spec:
  replicas: 3
  selector:
    matchLabels:
      app: dotnet-event-bus
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: dotnet-event-bus
    spec:
      containers:
      - name: eventbus
        image: registry.example.com/dotnet-event-bus:1.2.0
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 5000
        env:
        - name: DOTNET_ENVIRONMENT
          value: "Production"
        - name: EventBus__MaxConcurrentHandlers
          value: "4"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: dotnet-event-bus-service
  namespace: production
spec:
  selector:
    app: dotnet-event-bus
  ports:
  - port: 80
    targetPort: 5000
  type: LoadBalancer
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: dotnet-event-bus-hpa
  namespace: production
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: dotnet-event-bus
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

**Deploy:**
```bash
kubectl apply -f eventbus-deployment.yaml
kubectl rollout status deployment/dotnet-event-bus -n production
```

## Persistence Options

### PostgreSQL

```csharp
const string connectionString = "Server=localhost;Database=eventbus;User=eventbus;Password=password;";

services.AddEventBus(
    new PostgresEventMessageRepository(connectionString),
    new PostgresSubscriptionRepository(connectionString),
    new PostgresDeadLetterRepository(connectionString)
);
```

**Schema Creation:**
```sql
CREATE TABLE event_messages (
    id UUID PRIMARY KEY,
    event_type_name VARCHAR(255) NOT NULL,
    payload JSONB NOT NULL,
    metadata JSONB,
    correlation_id UUID,
    created_at TIMESTAMP NOT NULL,
    processed_at TIMESTAMP,
    retry_count INT DEFAULT 0
);

CREATE INDEX idx_event_type ON event_messages(event_type_name);
CREATE INDEX idx_created_at ON event_messages(created_at);
```

### MongoDB

```csharp
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("eventbus");

services.AddEventBus(
    new MongoEventMessageRepository(database),
    new MongoSubscriptionRepository(database),
    new MongoDeadLetterRepository(database)
);
```

### Redis (Caching Layer)

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

services.AddSingleton<IEventCache>(
    sp => new RedisEventCache(sp.GetRequiredService<IDistributedCache>())
);
```

## Monitoring & Logging

### Structured Logging

```csharp
services.AddLogging(builder =>
{
    builder
        .ClearProviders()
        .AddConsole()
        .AddJsonConsole();
});
```

**Log Example:**
```json
{
  "timestamp": "2026-05-04T10:30:45Z",
  "level": "Information",
  "message": "Event published",
  "event_id": "ev-123",
  "event_type": "OrderCreatedEvent",
  "handlers_invoked": 3,
  "duration_ms": 245,
  "correlation_id": "corr-456"
}
```

### Prometheus Metrics

```csharp
services.AddSingleton<IMetricsCollector>(
    sp => new PrometheusMetricsCollector()
);
```

**Metrics Endpoint:**
```
GET /metrics

# HELP eventbus_events_published_total Total events published
# TYPE eventbus_events_published_total counter
eventbus_events_published_total 1250

# HELP eventbus_handler_duration_seconds Handler execution duration
# TYPE eventbus_handler_duration_seconds histogram
eventbus_handler_duration_seconds_bucket{le="0.1"} 1200
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddEventBusHealthCheck()
    .AddDeadLetterQueueHealthCheck()
    .AddDbHealthCheck();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## Performance Tuning

### Memory Configuration

```csharp
services.AddEventBus(options =>
{
    // Tune for your workload
    options.MaxConcurrentHandlers = Environment.ProcessorCount;
    
    // Adjust cache size
    var cacheOptions = new InMemoryCacheOptions
    {
        SizeLimit = 10000,
        CompactionPercentage = 0.25
    };
});
```

### Batch Publishing Optimization

```csharp
// For high-throughput scenarios
var publisher = serviceProvider.GetRequiredService<IBatchEventPublisher>();

// Accumulate events
for (int i = 0; i < 1000; i++)
{
    await publisher.AddEventAsync(new MyEvent { Id = i });
}

// Publish all at once
await publisher.FlushAsync();
```

### Handler Optimization

```csharp
// Use TimeSpan.Zero for handlers that complete instantly
options.DefaultHandlerTimeout = TimeSpan.FromSeconds(30);

// Increase for long-running handlers
options.MaxRetryAttempts = 3;

// Reduce for strict time requirements
options.DefaultHandlerTimeout = TimeSpan.FromSeconds(5);
```

## Backup & Recovery

### Event Store Backup

```bash
# PostgreSQL backup
pg_dump -U eventbus eventbus > backup.sql

# PostgreSQL restore
psql -U eventbus eventbus < backup.sql
```

### Dead Letter Queue Recovery

```csharp
var dlq = serviceProvider.GetRequiredService<IDeadLetterService>();

// Get all pending entries
var pending = await dlq.GetPendingEntriesAsync();

// Bulk reprocess
foreach (var entry in pending)
{
    await dlq.ReprocessEntryAsync(entry.Id);
}
```

## Production Checklist

- [ ] Database configured and tested
- [ ] Connection strings secured (use Azure Key Vault, AWS Secrets Manager)
- [ ] Monitoring and alerting configured
- [ ] Health checks responding correctly
- [ ] Load testing completed
- [ ] Backup strategy implemented
- [ ] Logging and structured telemetry enabled
- [ ] Rate limiting configured
- [ ] Circuit breaker thresholds tuned
- [ ] Graceful shutdown implemented
- [ ] SSL/TLS enabled for all connections
- [ ] Authentication/authorization implemented at application level

## Troubleshooting Deployment

**Issue: High memory usage**
- Reduce `MaxConcurrentHandlers`
- Implement event pagination
- Monitor cache hit rates

**Issue: Event processing latency**
- Add more handler concurrency
- Scale horizontally
- Optimize database queries

**Issue: Dead letter queue growing**
- Investigate handler failures
- Check external service availability
- Increase retry delays

---

For questions, see `faq.md` or contact [@Sarmkadan](https://t.me/sarmkadan).
