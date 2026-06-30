# =============================================================================
# Dockerfile for DotnetEventBus
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Copy solution and project files
COPY DotnetEventBus.sln ./
COPY src/DotnetEventBus/DotnetEventBus.csproj ./src/DotnetEventBus/
COPY tests/DotnetEventBus.Tests/DotnetEventBus.Tests.csproj ./tests/DotnetEventBus.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ ./src/
COPY tests/ ./tests/

# Build in Release mode
RUN dotnet build -c Release --no-restore

# Run tests
RUN dotnet test tests/DotnetEventBus.Tests -c Release --no-build --logger "console;verbosity=minimal"

# Publish the library
RUN dotnet publish src/DotnetEventBus/DotnetEventBus.csproj \
    -c Release \
    --no-build \
    -o /app/publish

# Package the library
RUN dotnet pack src/DotnetEventBus/DotnetEventBus.csproj \
    -c Release \
    --no-build \
    -o /artifacts

# Runtime stage - minimal image for production
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1001 dotnetapp && chown -R dotnetapp:dotnetapp /app

# Copy published output
COPY --from=builder /app/publish ./

# Switch to non-root user
USER dotnetapp

# Expose port
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

CMD ["dotnet", "DotnetEventBus.dll"]

# Development stage - includes build tools
FROM builder AS development

WORKDIR /src

# Install development tools
RUN apt-get update && apt-get install -y \
    git \
    curl \
    vim \
    && rm -rf /var/lib/apt/lists/*

# Expose port for development
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

# Entry point for development
ENTRYPOINT ["/bin/bash"]

# Package stage - distributable NuGet package
FROM builder AS package

WORKDIR /artifacts

# The package is already built in the builder stage
COPY --from=builder /artifacts ./

ENTRYPOINT ["/bin/bash"]

# Final stage - optimized production image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production

WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published output from builder
COPY --from=builder /app/publish ./

# Create non-root user for security
RUN useradd -m -u 1001 dotnetapp && chown -R dotnetapp:dotnetapp /app

USER dotnetapp

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ENVIRONMENT=Production

# Metadata labels
LABEL maintainer="Vladyslav Zaiets <https://sarmkadan.com>"
LABEL description="DotnetEventBus - High-performance event bus for .NET"
LABEL version="2.0.0"

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

CMD ["dotnet", "DotnetEventBus.dll"]
