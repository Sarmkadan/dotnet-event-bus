# Cli

The `DotnetEventBus.Cli` namespace contains a small, text-oriented command layer for operating an event bus from a console host. `CommandLineInterface` is a dispatcher: it maps a command name to an `ICommand` implementation, runs it, and returns a `CommandResult` containing a success flag and a message. The library does not write to the console itself. Your host decides how to print the result and which exit code to return.

The namespace provides four built-in commands:

| Command | Class | Needs | Purpose |
|---------|-------|-------|---------|
| `publish` | `PublishCommand` | `IEventBus` | Publish a JSON event to the bus |
| `query` | `QueryCommand` | `IEventMessageRepository` | Read stored events of one type, with time and count filters |
| `stats` | `StatsCommand` | `MetricsCollector` | Show system, event, handler, or health metrics |
| `subscribe` | `SubscribeCommand` | nothing | Record, list, inspect, and remove subscription entries (see [Limitations](#limitations)) |

All command output is JSON unless noted otherwise. Failures return `Success = false` with a human-readable message.

## Types

### `CommandLineInterface`

```csharp
public sealed class CommandLineInterface
```

Dispatcher for registered commands.

| Member | Description |
|--------|-------------|
| `CommandLineInterface()` | Registers `publish`, `subscribe`, `query`, and `stats` with no dependencies. `publish`, `query`, and `stats` return a "not attached" failure until you supply the dependency, so use this form mainly for help output or `subscribe`. |
| `CommandLineInterface(IEventBus? eventBus, IEventMessageRepository? eventRepository = null, MetricsCollector? metrics = null)` | Registers the default commands, then replaces each one whose dependency is non-null with a bound version. A `null` dependency keeps the unbound command. |
| `void RegisterCommand(ICommand command)` | Adds or replaces a command. The name is matched case-insensitively. Throws `ArgumentNullException` if `command` is null. |
| `Task<CommandResult> ExecuteAsync(string commandName, string[] args)` | Runs a command. Returns a failure for a null or empty name, and for an unknown name. Exceptions thrown by a command are caught and returned as `Command execution failed: <message>`. |
| `string GetCommandHelp(string commandName)` | Returns the help text for one command, or `Command '<name>' not found`. |
| `IEnumerable<ICommand> GetAllCommands()` | Returns the registered commands in no guaranteed order. |

`ExecuteAsync` treats `help`, `--help`, and `-h` as built-in names. They return a list of commands, sorted by name, with their descriptions.

### `ICommand`

```csharp
public interface ICommand
{
    string Name { get; }
    string Description { get; }
    Task<CommandResult> ExecuteAsync(string[] args);
    string GetHelpText();
}
```

`args` contains only the arguments after the command name. Implement this interface to add your own commands.

### `CommandResult`

```csharp
public sealed class CommandResult
{
    public bool Success { get; }
    public string Message { get; }
    public CommandResult(bool success, string message);
}
```

`Message` is JSON on success for `publish`, `query`, `stats`, and `subscribe list`/`info`. Otherwise it is plain text, including usage messages.

## Commands

### `publish`

```
publish <event-type> <json-payload> [--metadata key=value ...]
```

Parses `<json-payload>`, wraps it in an `EventEnvelope` with `Source = "cli"`, and publishes the envelope through `IEventBus.PublishAsync<EventEnvelope>`. Subscriptions are keyed by CLR type, so `<event-type>` is stored only inside the envelope (`EventType`) and does not route the event. See [Behavior of published events](#behavior-of-published-events).

- Fewer than two arguments returns the usage text as a failure.
- Invalid JSON returns `Invalid JSON payload: <details>`. The JSON is parsed before the bus check, so invalid JSON is reported even when no bus is attached.
- Without an attached bus, it returns `No event bus is attached to this CLI...`.
- `--metadata key=value` adds an entry to `EventEnvelope.Metadata`. Repeat the option for more entries. Only text of the form `key=value` with exactly one `=` is accepted. Anything else after `--metadata` is skipped silently, and so is any unrecognized token.
- `Success` in the result is the `PublishResult.Success` value. A publish that fails inside the bus returns a failure with the same JSON body.

Output fields: `Success`, `EventId`, `EventType`, `HandlersInvoked`, `FailedHandlers`, `Error` (the `ErrorMessage` from the bus, or `null`), `Timestamp` (UTC, ISO 8601), and `MetadataCount`.

Example output (values illustrative):

```json
{
  "Success": true,
  "EventId": "3f0c1d2e-7a4b-4c8e-9d1f-2b5a6c7d8e9f",
  "EventType": "user.created",
  "HandlersInvoked": 2,
  "FailedHandlers": 0,
  "Error": null,
  "Timestamp": "2026-10-25T10:15:30.1234567Z",
  "MetadataCount": 2
}
```

### `query`

```
query <event-type> [--since <datetime>] [--until <datetime>] [--limit <count>]
```

Reads stored messages for `<event-type>` from `IEventMessageRepository`, then filters, orders, and limits them in memory. The repository is matched on the stored type name, which for CLI-published events is `DotnetEventBus.Models.EventEnvelope`, not the `<event-type>` given to `publish`. See [Behavior of published events](#behavior-of-published-events).

- The first argument is always the event type. Options are read from the second argument onward. An option is a `--name` token followed by a value that does not start with `--`. A `--name` without a value is ignored.
- `--since` and `--until` are parsed with the invariant culture. A value without a time zone is treated as UTC. Both bounds are inclusive, and both are compared against `CreatedAtUtc`.
- `--limit` defaults to `100` and must be a positive integer.
- Results are sorted newest first.
- Unknown options are ignored.

Output fields: `eventType`, `resultCount`, and `results`. Each entry in `results` has `id`, `type`, `timestamp` (ISO 8601 UTC), `correlationId`, `source`, and `data`. `data` is the stored payload string, so the JSON is escaped inside the value. For CLI-published events, `data` is the serialized envelope, and the user's JSON sits under its `Payload` field.

Example output (values illustrative, after `publish order.placed '{"orderId":456}'`):

```json
{
  "eventType": "DotnetEventBus.Models.EventEnvelope",
  "resultCount": 1,
  "results": [
    {
      "id": "e21120d0-0ed1-4ee6-8592-8e109fe60847",
      "type": "DotnetEventBus.Models.EventEnvelope",
      "timestamp": "2026-10-25T04:12:17.8821630Z",
      "correlationId": "70773d8a-936f-4daf-b651-81cb5e0cfdfd",
      "source": null,
      "data": "{\"EventType\":\"order.placed\",\"Payload\":{\"orderId\":456},\"Source\":\"cli\",\"Metadata\":{\"source\":\"api\"}}"
    }
  ]
}
```

The `data` value above is abbreviated to the fields that matter; the stored envelope contains more fields.

### `stats`

```
stats [system|events|handlers|health]
```

Reads `MetricsCollector` and returns JSON. With no argument, it shows `system`. The type is case-insensitive. An unknown type returns `Unknown stats type: <type>`.

| Type | Contents |
|------|----------|
| `system` (default) | `system`: `uptime` (`d.hh:mm:ss`), `startTime`, `status`, `throughputPerSecond`. `events`: `totalPublished`, `totalFailed`, `successRate`, `averageLatencyMs`. `handlers`: `registered`, `eventTypes`. |
| `events` | `topEvents`: up to 10 event types, ordered by publish count. Each has `type`, `count`, `failures`, `avgLatencyMs`. |
| `handlers` | `handlers`: every handler metric. Each has `name`, `eventType`, `eventsProcessed`, `failures`, `avgLatencyMs`. |
| `health` | `status`, `events` (`published`, `failed`, `successRate`), and `timestamp` (UTC). |

`status` is computed from the success rate:

- `Healthy` when nothing has been published yet.
- `Unhealthy` when the success rate is below 50.
- `Degraded` when it is at least 50 and below 95.
- `Healthy` otherwise.

Example output for `stats health` (values illustrative):

```json
{
  "status": "Degraded",
  "events": {
    "published": 120,
    "failed": 9,
    "successRate": 92.5
  },
  "timestamp": "2026-10-25T10:20:00.0000000Z"
}
```

### `subscribe`

```
subscribe add <event-type> <handler>
subscribe list
subscribe remove <subscription-id>
subscribe info <subscription-id>
```

Manages a list of subscription records.

- `add` returns a plain-text message with the new ID, which is the first 8 characters of a GUID.
- `list` returns a JSON array with `id`, `eventType`, `handler`, and `createdAt`. When the list is empty it returns `No subscriptions registered.`
- `remove` deletes every record with the ID. It returns a failure when the ID is not found.
- `info` returns one record as JSON. It always reports `status` as `Active`.

### Behavior of published events

- Routing: `publish` calls `PublishAsync<EventEnvelope>`, so only handlers subscribed to the `EventEnvelope` type run. Handlers subscribed to a type named after `<event-type>` do not run. `HandlersInvoked` is `0` when nothing matches, and the result is still `Success = true`.
- Storage: the bus stores each message under the CLR full name of the published type (`DotnetEventBus.Models.EventEnvelope`). `query` filters on that name, so use it as the `<event-type>` argument to find CLI-published events. The `<event-type>` string the user typed remains available in the stored envelope's `EventType` field.
- Source: the envelope's `Source` is `cli`. The message-level `source` shown by `query` is set separately by the bus and may be `null`.

## Limitations

The `subscribe` records are held in a `static` list that is shared by every `SubscribeCommand` instance in the process. The list is not persisted, has no lock, and is not connected to any `IEventBus`. `add` records the entry only. It does not register a handler, so an event published afterwards will not reach it. Treat `subscribe` as a bookkeeping stub until it is wired to the bus.

## Examples

### Create a CLI with a bus, a store, and metrics

```csharp
using System;
using System.Threading.Tasks;
using DotnetEventBus.Advanced;
using DotnetEventBus.Cli;
using DotnetEventBus.Configuration;
using DotnetEventBus.Repositories;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEventBus();

using var provider = services.BuildServiceProvider();

var bus = provider.GetRequiredService<IEventBus>();
var store = provider.GetRequiredService<IEventMessageRepository>();
var metrics = new MetricsCollector();

var cli = new CommandLineInterface(bus, store, metrics);
```

### Run a command from a console host

`ExecuteAsync` takes the command name and the remaining arguments as separate parameters. Print the message and set the exit code from `Success`.

```csharp
using System;
using System.Threading.Tasks;
using DotnetEventBus.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new CommandLineInterface(bus, store, metrics); // see the previous example

        if (args.Length == 0)
        {
            args = ["help"];
        }

        var result = await cli.ExecuteAsync(args[0], args[1..]);
        Console.WriteLine(result.Message);

        return result.Success ? 0 : 1;
    }
}
```

Run from a shell:

```bash
dotnet run -- publish user.created '{"userId":123,"email":"user@example.com"}' --metadata source=api
dotnet run -- query user.created --since 2026-10-01T00:00:00Z --limit 50
dotnet run -- stats health
```

### Publish with metadata from code

```csharp
var result = await cli.ExecuteAsync("publish", new[]
{
    "order.placed",
    "{\"orderId\":456}",
    "--metadata", "source=api",
    "--metadata", "version=1"
});

Console.WriteLine(result.Success ? "Published" : $"Failed: {result.Message}");
```

### Query a time window

Use the stored type name, because `order.placed` will not match CLI-published events (see below).

```csharp
var result = await cli.ExecuteAsync("query", new[]
{
    "DotnetEventBus.Models.EventEnvelope",
    "--since", "2026-10-24T00:00:00Z",
    "--until", "2026-10-25T00:00:00Z",
    "--limit", "20"
});
```

### Read stats and handle an unknown type

```csharp
var summary = await cli.ExecuteAsync("stats", Array.Empty<string>());   // system
var perEvent = await cli.ExecuteAsync("stats", new[] { "events" });

var bad = await cli.ExecuteAsync("stats", new[] { "latency" });
// bad.Success == false, bad.Message == "Unknown stats type: latency"
```

### Use a CLI without a bus

The parameterless constructor is useful when you only need help output or the `subscribe` command.

```csharp
var cli = new CommandLineInterface();

var help = await cli.ExecuteAsync("help", Array.Empty<string>());
Console.WriteLine(help.Message);

var publish = await cli.ExecuteAsync("publish", new[] { "x", "{}" });
// publish.Success == false, publish.Message says that no event bus is attached
```

### Add a custom command

```csharp
using System;
using System.Threading.Tasks;
using DotnetEventBus.Cli;

public sealed class PingCommand : ICommand
{
    public string Name => "ping";
    public string Description => "Check that the CLI is running";

    public Task<CommandResult> ExecuteAsync(string[] args)
    {
        return Task.FromResult(new CommandResult(true, "pong"));
    }

    public string GetHelpText()
    {
        return "Usage: ping\n\nDescription:\n  Returns pong.";
    }
}

var cli = new CommandLineInterface();
cli.RegisterCommand(new PingCommand());

var result = await cli.ExecuteAsync("PING", Array.Empty<string>()); // name is case-insensitive
Console.WriteLine(result.Message); // pong
```

### Look up help for one command

```csharp
Console.WriteLine(cli.GetCommandHelp("publish"));
Console.WriteLine(cli.GetCommandHelp("nope")); // Command 'nope' not found
```

## Related

- [CommandLineInterface](CommandLineInterface.md) - reference page for the dispatcher. Note that some of its descriptions are generic and do not match the current signatures. This page is the accurate reference for the `Cli` namespace.
- [MetricsCollector](MetricsCollector.md) - source of the `stats` output.
- [EventBus](EventBus.md) - implementation of `IEventBus` used by `publish`.
