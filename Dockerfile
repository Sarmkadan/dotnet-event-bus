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

# Package the library
RUN dotnet pack src/DotnetEventBus/DotnetEventBus.csproj \
    -c Release \
    --no-build \
    -o /artifacts

# Runtime stage - minimal image for deployment
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app

# Create non-root user
RUN useradd -m -u 1001 dotnetapp && chown -R dotnetapp:dotnetapp /app
USER dotnetapp

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD test -f /app/.health || exit 1

# Default command - used for example apps
CMD ["dotnet", "--version"]

# Development stage - includes build tools
FROM builder AS development

WORKDIR /src

# Install development tools
RUN apt-get update && apt-get install -y \
    git \
    curl \
    vim \
    && rm -rf /var/lib/apt/lists/*

# Expose port for example server
EXPOSE 5000

# Entry point for development
ENTRYPOINT ["/bin/bash"]

# Package stage - distributable NuGet package
FROM builder AS package

WORKDIR /artifacts

# The package is already built in the builder stage
# Copy it to artifacts directory
COPY --from=builder /artifacts ./

EXPOSE 5000

ENTRYPOINT ["/bin/bash"]

# Final stage - optimized production image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS production

WORKDIR /app

# Copy NuGet package from builder
COPY --from=builder /artifacts/ ./

# Create health marker for health check
RUN touch .health && chmod 644 .health

# Create non-root user for security
RUN useradd -m -u 1001 dotnetapp && chown -R dotnetapp:dotnetapp /app
USER dotnetapp

# Metadata labels
LABEL maintainer="Vladyslav Zaiets <https://sarmkadan.com>"
LABEL description="DotnetEventBus - High-performance event bus for .NET"
LABEL version="1.2.0"

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD test -f /app/.health || exit 1

CMD ["ls", "-la", "/app"]
