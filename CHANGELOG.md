# Changelog

All notable changes to the DotnetEventBus project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added
- Comprehensive documentation and examples (Phase 3)
- Dockerfile and docker-compose.yml for containerization
- CI/CD workflow for automated testing and deployment
- Makefile for build automation
- .editorconfig for consistent code style
- 8 complete example programs demonstrating all features
- Detailed architecture documentation
- FAQ and troubleshooting guides
- API reference documentation
- Getting started guide

### Changed
- Expanded README to 2000+ words with complete usage examples
- Improved error messages with detailed troubleshooting hints

### Documentation
- Added docs/ directory with comprehensive guides
- Added examples/ directory with production-ready samples

## [1.1.0] - 2026-04-15

### Added
- Advanced features and infrastructure (Phase 2)
- Middleware pipeline with composable architecture
- Event filtering with fluent API
- Event transformation and mapping
- Saga orchestration with compensation
- Event sourcing base classes
- Request-response pattern implementation
- Batch event publishing
- Performance profiling with percentile reporting
- Metrics collection and analysis
- Health check framework
- CLI command interface
- REST API endpoints
- Circuit breaker pattern
- Rate limiting middleware
- Multiple output formatters (JSON, CSV, XML)
- Webhook integration with HMAC-SHA256 signing
- In-memory caching with LRU eviction
- Dead letter processor worker
- 30+ utility extension methods
- Comprehensive error handling

### Performance
- Parallel handler execution with configurable limits
- Batch operations for improved throughput
- In-memory caching for frequently accessed data
- Optimized reflection for handler discovery

### Reliability
- Exponential backoff with jitter for retries
- Circuit breaker to prevent cascading failures
- Dead letter queue with automatic reprocessing
- Handler timeouts and cancellation support

## [1.0.0] - 2026-03-20

### Added
- Core event bus implementation (Phase 1)
- In-process pub-sub messaging
- Event handler registration and execution
- Subscription management
- Dead letter queue support
- Retry policies with exponential backoff
- Handler priorities and execution ordering
- Concurrent handler processing
- Message tracking and correlation IDs
- Async/await support throughout
- Dependency injection integration
- Exception handling and logging
- Type-safe event publishing
- Repository pattern for data persistence
- In-memory repository implementations
- Comprehensive unit tests

### Features
- Flexible configuration through EventBusOptions
- Fluent builder API for setup
- Support for class-based handlers
- Support for delegate handlers
- Polymorphic handler support
- Handler discovery through reflection
- Customizable exception handlers
- Event envelopes with metadata

### Architecture
- Clean separation of concerns
- Service-oriented design
- Pluggable repository implementations
- Extensible handler system
- Configurable middleware hooks

## [0.1.0] - 2026-03-01

### Initial Release
- Project scaffolding and setup
- Basic event bus interface definition
- Event message models
- Handler base classes
- Repository interfaces
- Initial unit tests
- MIT License

---

### Version Compatibility

| Version | .NET | Status | EOL |
|---------|------|--------|-----|
| 1.2.0 | 10.0 | Active | 2028-05-04 |
| 1.1.0 | 10.0 | Supported | 2027-04-15 |
| 1.0.0 | 10.0 | Maintained | 2027-03-20 |
| 0.1.0 | 10.0 | Outdated | 2026-06-01 |

### Breaking Changes

**1.0 → 1.1:**
- EventBusBuilder API expanded (backward compatible)
- Middleware now required in pipeline (optional in 1.0)

**1.1 → 1.2:**
- No breaking changes (fully backward compatible)

### Migration Guides

All versions are backward compatible within semantic versioning guidelines.
For detailed migration information, see docs/migration-guide.md.

### Credits

- **Author**: Vladyslav Zaiets (@Sarmkadan)
- **CTO & Software Architect**: https://sarmkadan.com
