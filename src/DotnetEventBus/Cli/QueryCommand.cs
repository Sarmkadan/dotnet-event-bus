// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetEventBus.Cli;

/// <summary>
/// CLI command for querying events and their history.
/// Supports filtering by event type and time range.
/// </summary>
public class QueryCommand : ICommand
{
    public string Name => "query";
    public string Description => "Query events and event history";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        await Task.CompletedTask; // Keep async signature

        if (args.Length == 0)
        {
            return new CommandResult(false, GetHelpText());
        }

        var eventType = args[0];
        var options = ParseOptions(args);

        try
        {
            // TODO: Query actual event store when available
            var mockResults = new[]
            {
                new {
                    id = "evt-001",
                    type = eventType,
                    timestamp = DateTime.UtcNow.AddMinutes(-5).ToString("o"),
                    data = new { id = 1, name = "Sample Event" }
                }
            };

            var json = JsonSerializer.Serialize(new
            {
                eventType = eventType,
                resultCount = mockResults.Length,
                results = mockResults
            }, new JsonSerializerOptions { WriteIndented = true });

            return new CommandResult(true, json);
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Query failed: {ex.Message}");
        }
    }

    public string GetHelpText()
    {
        return @"
Usage: query <event-type> [--since <datetime>] [--until <datetime>] [--limit <count>]

Description:
  Query events from the event store.

Arguments:
  <event-type>     The type of event to query

Options:
  --since          Start of time range (ISO 8601 format)
  --until          End of time range (ISO 8601 format)
  --limit          Maximum number of results (default: 100)

Examples:
  query user.created
  query user.created --limit 50
  query order.placed --since 2024-01-01T00:00:00Z --until 2024-01-02T00:00:00Z
";
    }

    private Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>();

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i].Substring(2);
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    options[key] = args[i + 1];
                    i++;
                }
            }
        }

        return options;
    }
}
