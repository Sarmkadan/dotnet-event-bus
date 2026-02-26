// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetEventBus.Cli;

/// <summary>
/// CLI command for publishing events.
/// Supports JSON payload and metadata options.
/// </summary>
public class PublishCommand : ICommand
{
    public string Name => "publish";
    public string Description => "Publish an event to the event bus";

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult(false, GetHelpText());
        }

        var eventType = args[0];
        var jsonPayload = args[1];
        var metadata = ParseMetadata(args.Skip(2).ToArray());

        try
        {
            // Parse JSON payload
            using (var doc = JsonDocument.Parse(jsonPayload))
            {
                var eventData = doc.RootElement;

                // TODO: Publish to actual event bus when available
                var result = new
                {
                    Success = true,
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    MetadataCount = metadata.Count
                };

                return new CommandResult(true, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch (JsonException ex)
        {
            return new CommandResult(false, $"Invalid JSON payload: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error publishing event: {ex.Message}");
        }
    }

    public string GetHelpText()
    {
        return @"
Usage: publish <event-type> <json-payload> [--metadata key=value ...]

Description:
  Publishes an event to the event bus.

Arguments:
  <event-type>     The type of event to publish (e.g., 'user.created')
  <json-payload>   JSON object containing the event data

Options:
  --metadata       Additional metadata as key=value pairs

Examples:
  publish user.created '{""userId"":123,""email"":""user@example.com""}'
  publish order.placed '{""orderId"":456}' --metadata source=api --metadata version=1
";
    }

    private Dictionary<string, string> ParseMetadata(string[] args)
    {
        var metadata = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--metadata" && i + 1 < args.Length)
            {
                var keyValue = args[i + 1].Split('=');
                if (keyValue.Length == 2)
                {
                    metadata[keyValue[0]] = keyValue[1];
                }

                i++;
            }
        }

        return metadata;
    }
}
