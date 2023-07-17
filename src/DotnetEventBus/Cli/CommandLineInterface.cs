#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetEventBus.Cli;

/// <summary>
/// Command-line interface for the event bus.
/// Provides commands for publishing, subscribing, querying, and managing events.
/// Why: Enables system operators to interact with the event bus without code.
/// </summary>
public sealed class CommandLineInterface
{
    private readonly Dictionary<string, ICommand> _commands = [];
    private readonly StringBuilder _helpText = new();

    public CommandLineInterface()
    {
        RegisterDefaultCommands();
    }

    /// <summary>
    /// Creates a CLI whose default commands are wired to concrete services.
    /// Commands whose dependency is not supplied fall back to their unbound variant.
    /// </summary>
    public CommandLineInterface(
        Services.IEventBus? eventBus,
        Repositories.IEventMessageRepository? eventRepository = null,
        Advanced.MetricsCollector? metrics = null)
    {
        RegisterDefaultCommands();

        if (eventBus is not null)
        {
            RegisterCommand(new PublishCommand(eventBus));
        }

        if (eventRepository is not null)
        {
            RegisterCommand(new QueryCommand(eventRepository));
        }

        if (metrics is not null)
        {
            RegisterCommand(new StatsCommand(metrics));
        }
    }

    /// <summary>
    /// Registers a command.
    /// </summary>
    public void RegisterCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands[command.Name.ToLower()] = command;
    }

    /// <summary>
    /// Executes a command with the given arguments.
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(string commandName, string[] args)
    {
        if (string.IsNullOrEmpty(commandName))
        {
            return new CommandResult(false, "Command name is required");
        }

        if (commandName.Equals("help", StringComparison.OrdinalIgnoreCase) || commandName == "--help" || commandName == "-h")
        {
            return new CommandResult(true, GetHelpText());
        }

        if (!_commands.TryGetValue(commandName.ToLower(), out var command))
        {
            return new CommandResult(false, $"Unknown command: {commandName}. Type 'help' for available commands.");
        }

        try
        {
            var result = await command.ExecuteAsync(args);
            return result;
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Command execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the help text for a specific command.
    /// </summary>
    public string GetCommandHelp(string commandName)
    {
        if (_commands.TryGetValue(commandName.ToLower(), out var command))
        {
            return command.GetHelpText();
        }

        return $"Command '{commandName}' not found";
    }

    /// <summary>
    /// Gets all available commands.
    /// </summary>
    public IEnumerable<ICommand> GetAllCommands()
    {
        return _commands.Values;
    }

    private void RegisterDefaultCommands()
    {
        RegisterCommand(new PublishCommand());
        RegisterCommand(new SubscribeCommand());
        RegisterCommand(new QueryCommand());
        RegisterCommand(new StatsCommand());
    }

    private string GetHelpText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("DotNet Event Bus CLI");
        sb.AppendLine("===================");
        sb.AppendLine();
        sb.AppendLine("Available Commands:");
        sb.AppendLine();

        foreach (var command in _commands.Values.OrderBy(c => c.Name))
        {
            sb.AppendLine($"  {command.Name} - {command.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("Use 'help <command>' for more information about a command.");
        return sb.ToString();
    }
}

/// <summary>
/// Interface for CLI commands.
/// </summary>
public interface ICommand
{
    string Name { get; }
    string Description { get; }

    Task<CommandResult> ExecuteAsync(string[] args);
    string GetHelpText();
}

/// <summary>
/// Result of command execution.
/// </summary>
public sealed class CommandResult
{
    public bool Success { get; }
    public string Message { get; }

    public CommandResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}
