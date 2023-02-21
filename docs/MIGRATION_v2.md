# Migration Guide: v1.x to v2.0

This document covers the breaking changes and migration steps for upgrading DotnetEventBus from v1.x to v2.0.

## Overview

Version 2.0 introduces Docker-first deployment, updated port conventions, and improved production defaults. The library API remains backward-compatible - most breaking changes are in configuration and infrastructure.

## Breaking Changes

### 1. Default Port Changed from 5000 to 8080

All Docker images and compose services now use port 8080 by default, aligning with the .NET 10 convention and container best practices.

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

## Non-Breaking Changes

### Improved Defaults

- `start-period` for health checks increased from 5s to 10s for more reliable cold starts
- Non-root user setup improved with proper file ownership
- Environment variables `DOTNET_ENVIRONMENT` and `ASPNETCORE_URLS` are set explicitly in all stages

### Docker Compose Updates

- All services now consistently use port 8080
- Example API service maps to host port 8081 to avoid conflicts with the production service
- Health checks in compose file updated to use HTTP-based checks

## Step-by-Step Migration

1. **Update your docker-compose overrides** - Replace any port 5000 references with 8080
2. **Update reverse proxy** - Point upstream to port 8080
3. **Verify health endpoint** - Ensure `/health` is mapped in your application
4. **Rebuild images** - `docker-compose build --no-cache`
5. **Test locally** - `docker-compose up production` and verify health check passes
6. **Update Kubernetes manifests** - Change `containerPort` from 5000 to 8080
7. **Update CI/CD pipelines** - Adjust any port references in deployment scripts

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
| API | Compatible | Compatible |

## Questions

See `faq.md` or contact [@Sarmkadan](https://t.me/sarmkadan).
