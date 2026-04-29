# Docker Guide for DotnetEventBus

This guide provides comprehensive instructions for using Docker with DotnetEventBus, including quick start instructions, Docker Compose usage, environment variables, and production deployment best practices.

## Quick Start with Docker

### Prerequisites

- Docker 20.10+ installed
- Docker Compose 3.8+
- .NET 10.0 SDK (for development)

### Building and Running

#### 1. Quick Start with Pre-built Images

```bash
# Pull the latest image
docker pull ghcr.io/sarmkadan/dotnet-event-bus:latest

# Run the container
docker run -d -p 8080:8080 ghcr.io/sarmkadan/dotnet-event-bus:latest
```

#### 2. Build from Source

```bash
# Clone the repository
git clone https://github.com/Sarmkadan/dotnet-event-bus.git
cd dotnet-event-bus

# Build the development image
docker build -t dotnet-event-bus:latest .

# Run the container
docker run -p 8080:8080 dotnet-event-bus:latest
```

## Docker Compose Usage

### Development Environment

```yaml
version: '3.8'

services:
  eventbus-dev:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    ports:
      - "8080:8080"
    volumes:
      - .:/app
    environment:
      - DOTNET_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080

  eventbus-test:
    build:
      context: .
      dockerfile: Dockerfile
      target: test
    environment:
      - DOTNET_ENVIRONMENT=Test

# Example docker-compose.yml
version: '3.8'

services:
  # Development service with hot reload
  dev:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    ports:
      - "8080:8080"
    volumes:
      - .:/app
    environment:
      - DOTNET_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080

  # Production service
  production:
    build:
      context: .
      dockerfile: Dockerfile
      target: production
    ports:
      - "8080:8080"
    environment:
      - DOTNET_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080

  # Test service
  test:
    build:
      context: .
      dockerfile: Dockerfile
      target: test
    environment:
      - DOTNET_ENVIRONMENT=Test
```

### Multi-Stage Docker Build

The DotnetEventBus Dockerfile uses a multi-stage build process to optimize image size and security:

```dockerfile
# Build stage - restore and build the project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "DotnetEventBus.sln"
RUN dotnet build "DotnetEventBus.sln" -c Release --no-restore

# Runtime stage - optimized production image
FROM build AS runtime
WORKDIR /app
EXPOSE 8080

# Development stage - includes debugging tools
FROM runtime AS development
# Copy app to workspace
COPY --from=build /src/bin/Release/net10.0/publish/ /app/
WORKDIR /app
ENTRYPOINT ["dotnet", "DotnetEventBus.dll"]

# Production stage - minimal image for production
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY --from=build /src/bin/Release/net10.0/publish/ /app/
EXPOSE 8080
HEALTHCHECK CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "DotnetEventBus.dll"]
```

## Environment Variables Reference

### Core Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|----------|-----------|
| `DOTNET_ENVIRONMENT` | .NET runtime environment | Production | No |
| `ASPNETCORE_URLS` | URLs to bind to | http://+:8080 | Yes |
| `EventBus__MaxConcurrentHandlers` | Maximum concurrent handlers | 4 | No |
| `EventBus__EnableDeadLetterQueue` | Enable dead letter queue | true | No |
| `EventBus__EnableEventReplay` | Enable event replay feature | true | No |

### Health and Monitoring

| Variable | Description | Default |
|----------|-------------|----------|
| `HEALTHCHECK_ENABLED` | Enable health checks | true |
| `HEALTHCHECK_PATH` | Health check endpoint path | /health |
| `LOG_LEVEL` | Logging level | Information |

### Performance Tuning

| Variable | Description | Default |
|----------|-------------|----------|
| `EventBus__MaxConcurrentHandlers` | Max concurrent handlers | CPU count |
| `EventBus__HandlerTimeout` | Default handler timeout (seconds) | 30 |
| `EventBus__MaxReplayConcurrency` | Max replay concurrency | 4 |

### Example Environment Configuration

```bash
# Development environment
docker run -d \
  -e DOTNET_ENVIRONMENT=Development \
  -e EventBus__MaxConcurrentHandlers=8 \
  -e EventBus__EnableDeadLetterQueue=true \
  -e EventBus__EnableEventReplay=true \
  -p 8080:8080 \
  dotnet-event-bus:latest
```

## Production Deployment Checklist

### 1. Container Configuration

```dockerfile
# Production-optimized Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY . .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
USER containeruser
ENTRYPOINT ["dotnet", "DotnetEventBus.dll"]
```

### 2. Security Configuration

```dockerfile
# Create non-root user
RUN adduser -u 1000 -D -S containeruser
USER containeruser
```

### 3. Resource Limits

```yaml
version: '3.8'

services:
  eventbus:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    ports:
      - "8080:8080"
    environment:
      - DOTNET_ENVIRONMENT=Production
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
        reservations:
          memory: 256M
          cpus: '0.25'
```

### 4. Health Check Configuration

```csharp
// In your application startup
app.UseRouting();
app.MapHealthChecks("/health");
app.MapControllers(); // or MapEventBusEndpoints()
```

### 5. Persistent Storage

```yaml
services:
  eventbus-db:
    image: postgres:13
    environment:
      POSTGRES_DB: eventbus
      POSTGRES_USER: eventbus_user
      POSTGRES_PASSWORD: your_secure_password
    volumes:
      - postgres_data:/var/lib/postgresql/data

  eventbus:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    depends_on:
      - eventbus-db
    environment:
      - DATABASE_URL=postgresql://eventbus_user:your_secure_password@eventbus-db:5432/eventbus
      - DOTNET_ENVIRONMENT=Production
    ports:
      - "8080:8080"
```

## Docker Compose Examples

### Basic Single Service

```yaml
version: '3.8'

services:
  eventbus:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    ports:
      - "8080:8080"
    environment:
      - DOTNET_ENVIRONMENT=Production
    restart: unless-stopped
```

### Load Balanced Setup

```yaml
version: '3.8'

services:
  load-balancer:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - eventbus1
      - eventbus2
      - eventbus3

  eventbus1:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    environment:
      - DOTNET_ENVIRONMENT=Production
    # No ports exposed - internal service

  eventbus2:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    environment:
      - DOTNET_ENVIRONMENT=Production
    # No ports exposed - internal service

  eventbus3:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    environment:
      - DOTNET_ENVIRONMENT=Production
    # No ports exposed - internal service
```

### Development with Hot Reload

```yaml
version: '3.8'

services:
  eventbus-dev:
    build:
      context: .
      dockerfile: Dockerfile
      target: development
    ports:
      - "8080:8080"
    volumes:
      - .:/app
    environment:
      - DOTNET_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    user: "${UID:-0}:${GID:-0}"
```

## Monitoring and Logging

### Structured Logging

```csharp
// Enable structured logging
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddJsonConsole(options =>
    {
        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        options.UseUtcTimestamp = true;
    });
});
```

### Health Monitoring

```csharp
// Add comprehensive health checks
services.AddHealthChecks()
    .AddEventBus("EventBus")
    .AddEventStore("EventStore")
    .AddDatabase("Database");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Resource Management

```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  eventbus:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
    restart: unless-stopped
```

## Troubleshooting Docker Issues

### Common Issues and Solutions

#### Issue: Container fails to start
**Solution:** Check port conflicts and ensure 8080 is available:
```bash
# Check if port is in use
lsof -i :8080

# Kill process using the port
fuser -k 8080/tcp
```

#### Issue: Health check fails
**Solution:** Ensure health endpoint is configured:
```csharp
app.MapHealthChecks("/health");
```

#### Issue: High memory usage
**Solution:** Configure resource limits:
```yaml
deploy:
  resources:
    limits:
      memory: 512M
    reservations:
      memory: 256M
```

#### Issue: Performance issues
**Solution:** Tune concurrency settings:
```bash
docker run -e EventBus__MaxConcurrentHandlers=4 \
  -e EventBus__HandlerTimeout=15 \
  -p 8080:8080 \
  dotnet-event-bus:latest
```

## Best Practices

### 1. Security
- Always run as non-root user
- Use multi-stage builds to reduce attack surface
- Scan images for vulnerabilities
- Keep base images updated

### 2. Performance
- Use resource limits in production
- Enable health checks for orchestration
- Monitor memory and CPU usage
- Use appropriate logging levels

### 3. Monitoring
- Implement structured logging
- Monitor health check endpoints
- Set up alerting for performance metrics
- Use resource monitoring

### 4. Configuration
- Use environment variables for configuration
- Implement proper secret management
- Use production-ready base images
- Enable proper error handling and recovery

## Advanced Configuration

### Custom Event Handlers in Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy custom handlers
COPY src/Handlers ./Handlers
RUN dotnet publish -c Release -o ../out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /src/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DotnetEventBus.dll"]
```

### Multi-Container Setup

```yaml
version: '3.8'

services:
  eventbus:
    image: ghcr.io/sarmkadan/dotnet-event-bus:latest
    environment:
      - DOTNET_ENVIRONMENT=Production
      - EventBus__MaxConcurrentHandlers=8
    deploy:
      replicas: 3
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  eventbus-db:
    image: postgres:13
    environment:
      POSTGRES_DB: eventbus
      POSTGRES_USER: eventbus_user
    volumes:
      - postgres_data:/var/lib/postgresql/data
```

## References

- **Docker Documentation**: https://docs.docker.com
- **Docker Compose**: https://docs.docker.com/compose/
- **.NET Docker Images**: https://hub.docker.com/_/microsoft-dotnet/
- **Health Checks**: Built-in health monitoring endpoints
- **Event Replay**: Time-travel event processing capabilities

For more information, see the main documentation at `README.md` and `docs/` directory.