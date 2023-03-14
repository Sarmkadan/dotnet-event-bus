# Contributing to DotnetEventBus

Thank you for considering contributing to DotnetEventBus!

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A git client

## Building Locally

```bash
# Clone the repository
git clone https://github.com/sarmkadan/dotnet-event-bus.git
cd dotnet-event-bus

# Restore dependencies
dotnet restore

# Build in Release configuration
dotnet build --configuration Release

# Or use the Makefile shortcut
make build
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with TRX report
dotnet test --logger "trx;LogFileName=test-results.trx"

# Or use the Makefile shortcut
make test
```

## How to Contribute

### 1. Fork and Clone

Fork the repository on GitHub, then clone your fork:

```bash
git clone https://github.com/your-username/dotnet-event-bus.git
cd dotnet-event-bus
```

### 2. Create a Branch

Branch off `main` with a descriptive name:

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-description
```

### 3. Make Your Changes

- Write focused, well-scoped changes.
- Keep commits small and with clear messages following [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `ci:`).
- Add or update tests for any code you change.
- Provide XML doc comments for all public APIs.

### 4. Run Checks

Before submitting, ensure builds and tests pass:

```bash
dotnet build --configuration Release
dotnet test --configuration Release
```

### 5. Submit a Pull Request

```bash
git push origin feature/your-feature-name
```

Open a Pull Request against `main`. Fill in the PR template and link any relevant issues.

## Code Style

- Follow `.editorconfig` settings (enforced automatically by most editors).
- Indent with 4 spaces for C# files.
- Use `file`-scoped namespaces.
- Enable nullable reference types; avoid `!` null-forgiving operators unless justified.
- Keep lines within 120 characters.

## Reporting Issues

Use [GitHub Issues](https://github.com/sarmkadan/dotnet-event-bus/issues). Include:
- A minimal reproduction case
- Expected vs. actual behavior
- .NET SDK version (`dotnet --version`)

## License

By contributing, you agree your contributions will be licensed under the [MIT License](LICENSE).
