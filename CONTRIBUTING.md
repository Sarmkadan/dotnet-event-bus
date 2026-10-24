# Contributing to DotnetEventBus

Contributions are welcome. This guide covers setup, the checks your change must pass, and how to open a pull request.

## Prerequisites

- **.NET SDK 10.0.100 or later in the 10.0 band.** `global.json` pins `10.0.100` with `rollForward: latestMinor`, so any newer 10.0.x SDK is accepted. CI uses `10.0.x`.
- `git`
- Optional: `make` (the `Makefile` wraps the `dotnet` commands below).

## Build

```bash
dotnet restore DotnetEventBus.sln
dotnet build DotnetEventBus.sln -c Release
```

Or, with make: `make build` (Debug) / `make release` (Release).

## Run Tests

```bash
dotnet test DotnetEventBus.sln -c Release
```

Run a single test class:

```bash
dotnet test --filter "FullyQualifiedName~ClassName"
```

Or `make test`, `make test-verbose`, and `make test-coverage`. Tests use xUnit, FluentAssertions, and Moq.

## Coding Conventions

- **Formatting and style:** follow `.editorconfig`: 4-space indentation, 120-column lines, file-scoped namespaces, braces required, `var` when the type is apparent. Run `make format` (`dotnet format`) before committing.
- **Nullable reference types** are enabled for every project (`Directory.Build.props`). Do not disable them or suppress nullability warnings without a reason.
- **Warnings:** do not add new build warnings. The build does not currently treat warnings as errors (`TreatWarningsAsErrors` is `false`), so CI will not catch them for you. `make lint` builds with `EnforceCodeStyleInBuild=true` and is the stricter check.
- **Public APIs:** interfaces are prefixed `I`, async methods end in `Async` and accept a `CancellationToken`, and every new public type or member has XML doc comments.
- **Tests:** one test file per class, named `XxxTests.cs`, in the flat `tests/DotnetEventBus.Tests/` folder. Name test methods `Method_Scenario_ExpectedResult`, for example `Serialize_NullData_ThrowsArgumentNullException`.
- **Docs:** new public types get an entry in `docs/` and, where it helps, a usage example in `examples/`.

## Pull Request Process

1. **Branch from `main`.** Use a short, descriptive name such as `feat/retry-backoff` or `fix/dlq-null-entry`.
2. **Keep PRs small and focused.** One logical change per PR. Split refactors from behaviour changes.
3. **Write conventional commit messages**, for example `feat:`, `fix:`, `docs:`, `test:`, `chore:`.
4. **Add or update tests** for any new or changed behaviour.
5. **Make sure CI passes.** The `CI` workflow (`.github/workflows/ci.yml`) runs `dotnet restore`, `dotnet build -c Release`, and `dotnet test -c Release` on every PR targeting `main`. Run the same commands locally first.
6. **Describe the change in the PR.** Explain what changed and why, how you tested it, and any breaking changes or follow-up work. Link related issues.

## Reporting Issues

Use GitHub Issues to report bugs or request features. Include a minimal reproduction case and your .NET SDK version (`dotnet --version`).

By contributing, you agree to license your work under the [MIT License](LICENSE).
