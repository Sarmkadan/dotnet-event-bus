# CLAUDE.md

DotnetEventBus - in-process publish/subscribe event bus library for .NET (C#, .NET 10) with middleware, retry, dead-letter queue, sagas, batch publishing and DI integration.

## Build

- SDK: .NET 10.0.100 (`global.json`, rollForward latestMinor)
- `dotnet restore` / `dotnet build DotnetEventBus.sln -c Release`
- `make build` / `make release` / `make clean` (Makefile wraps the same commands)
- Docker: `docker-compose.yml`, `Dockerfile`

## Test

- Framework: xUnit + FluentAssertions + Moq, project `tests/DotnetEventBus.Tests`
- `dotnet test DotnetEventBus.sln -c Release` (CI: `.github/workflows/ci.yml`)
- `make test`, `make test-verbose`, `make test-coverage`
- Single test: `dotnet test --filter "FullyQualifiedName~ClassName"` or `make test-file FILE=...`
- Benchmarks: `benchmarks/DotnetEventBus.Benchmarks` (BenchmarkDotNet), `make benchmark`

## Lint / Format

- `dotnet format DotnetEventBus.sln` (`make format`)
- `make lint` = build with `/p:EnforceCodeStyleInBuild=true`
- Rules in `.editorconfig`: 4-space indent, 120 col, file-scoped namespaces, braces required, `var` when type apparent
- `make verify` = clean + restore + format + lint + build + test

## Layout

- `src/DotnetEventBus/` - library (single project, `Directory.Build.props`: Nullable + ImplicitUsings on, warnings not errors)
  - `Services/` - core: `IEventBus`, `EventBus`, `SubscriptionManager`, `HandlerInvoker`, `BatchEventPublisher`, `DeadLetterService`
  - `Models/` - `EventEnvelope`, `EventMessage`, `PublishResult`, `Subscription`, `DeadLetterEntry`
  - `Configuration/` - `EventBusOptions`, DI extensions (`AddEventBus`)
  - `Middleware/`, `Handlers/`, `Repositories/`, `Transport/`, `Advanced/` (sagas, event sourcing, filters, request/response), `Monitoring/`, `Performance/`, `Caching/`, `Api/`, `Cli/`, `Workers/`, `Utilities/`, `Exceptions/`, `Formatters/`
  - `EventBusBuilder.cs` - fluent builder entry point
- `tests/DotnetEventBus.Tests/` - flat, one test file per class (`XxxTests.cs`)
- `examples/` - numbered usage samples (`01_BasicPubSub.cs` ...), `examples/v2-basic-usage/` runnable app
- `docs/` - per-class markdown reference; `README.md` is the aggregated doc
- `.github/workflows/` - ci, build, codeql, docker, nuget-publish, release

## Conventions

- Consumers use DI: `services.AddEventBus(options => ...)` then resolve `IEventBus`; `PublishAsync` returns `PublishResult`
- Handlers: delegate `Subscribe<TEvent>(Func<TEvent, CancellationToken, Task>, handlerName, priority)` or class-based `Subscribe<TEvent, THandler>()`
- Companion partial/extension files follow the pattern `XxxValidation.cs`, `XxxExtensions.cs`, `XxxJsonExtensions.cs` beside the main type
- Interfaces prefixed `I`, async methods suffixed `Async`, always accept `CancellationToken`
- Every public type gets a matching `tests/...Tests.cs`, `docs/Xxx.md` entry and often a benchmark
- Commit style: conventional commits (`feat:`, `fix:`, `docs:`, `chore:`)
