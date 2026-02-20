#nullable enable

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
/// CLI command for managing event subscriptions.
/// Allows creating, listing, and removing subscriptions.
/// </summary>
public sealed class SubscribeCommand : ICommand
{
    public string Name => "subscribe";
    public string Description => "Manage event subscriptions";

    private static readonly List<(string Id, string EventType, string Handler, DateTime CreatedAt)> _subscriptions = [];

    public async Task<CommandResult> ExecuteAsync(string[] args)
    {
        await Task.CompletedTask; // Keep async signature

        if (args.Length == 0)
        {
            return new CommandResult(false, GetHelpText());
        }

        var subcommand = args[0].ToLower();

        return subcommand switch
        {
            "add" => HandleAdd(args.Skip(1).ToArray()),
            "list" => HandleList(),
            "remove" => HandleRemove(args.Skip(1).ToArray()),
            "info" => HandleInfo(args.Skip(1).ToArray()),
            _ => new CommandResult(false, $"Unknown subcommand: {subcommand}")
        };
    }

    public string GetHelpText()
    {
        return @"
Usage: subscribe <subcommand> [options]

Description:
  Manage event subscriptions.

Subcommands:
  add <event-type> <handler>   Add a subscription
  list                         List all subscriptions
  remove <subscription-id>     Remove a subscription
  info <subscription-id>       Show subscription details

Examples:
  subscribe add user.created MyUserCreatedHandler
  subscribe list
  subscribe remove sub-123
";
    }

    private CommandResult HandleAdd(string[] args)
    {
        if (args.Length < 2)
        {
            return new CommandResult(false, "Usage: subscribe add <event-type> <handler>");
        }

        var eventType = args[0];
        var handler = args[1];
        var id = Guid.NewGuid().ToString().Substring(0, 8);

        _subscriptions.Add((id, eventType, handler, DateTime.UtcNow));

        return new CommandResult(true, $"Subscription created: {id}\nEvent Type: {eventType}\nHandler: {handler}");
    }

    private CommandResult HandleList()
    {
        if (_subscriptions.Count == 0)
        {
            return new CommandResult(true, "No subscriptions registered.");
        }

        var json = JsonSerializer.Serialize(_subscriptions.Select(s => new
        {
            id = s.Id,
            eventType = s.EventType,
            handler = s.Handler,
            createdAt = s.CreatedAt.ToString("o")
        }), new JsonSerializerOptions { WriteIndented = true });

        return new CommandResult(true, json);
    }

    private CommandResult HandleRemove(string[] args)
    {
        if (args.Length == 0)
        {
            return new CommandResult(false, "Usage: subscribe remove <subscription-id>");
        }

        var id = args[0];
        var removed = _subscriptions.RemoveAll(s => s.Id == id);

        if (removed > 0)
        {
            return new CommandResult(true, $"Subscription '{id}' removed.");
        }

        return new CommandResult(false, $"Subscription '{id}' not found.");
    }

    private CommandResult HandleInfo(string[] args)
    {
        if (args.Length == 0)
        {
            return new CommandResult(false, "Usage: subscribe info <subscription-id>");
        }

        var id = args[0];
        var subscription = _subscriptions.FirstOrDefault(s => s.Id == id);

        if (subscription == default)
        {
            return new CommandResult(false, $"Subscription '{id}' not found.");
        }

        var json = JsonSerializer.Serialize(new
        {
            id = subscription.Id,
            eventType = subscription.EventType,
            handler = subscription.Handler,
            createdAt = subscription.CreatedAt.ToString("o"),
            status = "Active"
        }, new JsonSerializerOptions { WriteIndented = true });

        return new CommandResult(true, json);
    }
}
