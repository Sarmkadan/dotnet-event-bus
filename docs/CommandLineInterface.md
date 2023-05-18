# CommandLineInterface

The `CommandLineInterface` class serves as the primary entry point for defining, registering, and executing console-based commands within the `dotnet-event-bus` ecosystem. It acts as a dispatcher that maps input arguments to registered `ICommand` implementations, manages execution flow, and aggregates results into a standardized `CommandResult` structure. This component is designed to facilitate modular CLI tools where commands can be dynamically added and invoked with consistent error handling and help documentation generation.

## API

### `public CommandLineInterface`
Initializes a new instance of the `CommandLineInterface` class. This constructor sets up the internal registry required to store command definitions and prepares the instance for command registration and execution.

### `public void RegisterCommand`
Registers a specific command implementation with the interface.
*   **Purpose**: Adds an `ICommand` instance to the internal collection, making it available for execution via `ExecuteAsync`.
*   **Parameters**: Accepts an instance of a class implementing `ICommand`. (Specific parameter types are inferred from standard registration patterns, typically the command instance itself).
*   **Return Value**: `void`.
*   **Exceptions**: May throw an exception if a command with the same identifier is already registered, though specific exception types depend on the internal implementation of the collision policy.

### `public async Task<CommandResult> ExecuteAsync`
Parses input arguments and executes the corresponding registered command.
*   **Purpose**: Identifies the target command based on provided arguments, invokes its logic, and returns the outcome.
*   **Parameters**: Typically accepts a string array representing command-line arguments (e.g., `string[] args`).
*   **Return Value**: Returns a `Task<CommandResult>` which resolves to a `CommandResult` object containing the execution status and output message.
*   **Exceptions**: May throw exceptions if the command execution fails critically, if no matching command is found, or if argument parsing encounters invalid formats.

### `public string GetCommandHelp`
Generates a help string for a specific command or the entire interface.
*   **Purpose**: Retrieves usage instructions, descriptions, and parameter details for registered commands.
*   **Parameters**: Usually accepts a command name or identifier; if null or empty, it may return general help for all commands.
*   **Return Value**: Returns a `string` containing formatted help text.
*   **Exceptions**: May throw if the requested command name does not exist in the registry.

### `public IEnumerable<ICommand> GetAllCommands`
Retrieves the collection of all currently registered commands.
*   **Purpose**: Provides read-only access to the list of commands available in the current interface instance.
*   **Parameters**: None.
*   **Return Value**: Returns an `IEnumerable<ICommand>`.
*   **Exceptions**: Generally does not throw unless the internal collection is modified concurrently during enumeration.

### `public bool Success`
Indicates the success status of the last executed operation or the current state of the result context.
*   **Purpose**: A boolean flag used to quickly determine if the most recent command execution completed without errors.
*   **Parameters**: None (Property getter).
*   **Return Value**: `true` if the operation was successful; otherwise, `false`.

### `public string Message`
Contains the output message or error description associated with the last operation.
*   **Purpose**: Provides human-readable feedback, such as success notifications, error details, or command output.
*   **Parameters**: None (Property getter).
*   **Return Value**: A `string` representing the result message.

### `public CommandResult`
Represents the structured outcome of a command execution.
*   **Purpose**: Encapsulates the `Success` status and `Message` content into a single return object for `ExecuteAsync`.
*   **Parameters**: N/A (This refers to the type definition used as a return type).
*   **Return Value**: N/A (Type definition).
*   **Exceptions**: N/A.

## Usage

### Example 1: Basic Command Registration and Execution
This example demonstrates initializing the interface, registering a custom command, and executing it with arguments.

```csharp
using System;
using System.Threading.Tasks;

// Assume ICommand and ConcreteCommand are defined elsewhere in the project
public class Program
{
    public static async Task Main(string[] args)
    {
        var cli = new CommandLineInterface();
        
        // Register a command instance
        cli.RegisterCommand(new PublishEventCommand());

        // Execute with simulated arguments
        var result = await cli.ExecuteAsync(args);

        // Handle the result
        if (result.Success)
        {
            Console.WriteLine($"Operation completed: {result.Message}");
        }
        else
        {
            Console.Error.WriteLine($"Execution failed: {result.Message}");
        }
    }
}
```

### Example 2: Generating Help Documentation
This example shows how to retrieve help text for a specific command or the full list of available commands.

```csharp
using System;
using System.Linq;

public class HelpHelper
{
    public static void DisplayHelp(CommandLineInterface cli, string commandName)
    {
        try
        {
            // Get help for a specific command
            string helpText = cli.GetCommandHelp(commandName);
            Console.WriteLine(helpText);
        }
        catch (Exception ex)
        {
            // Fallback to listing all commands if specific help fails
            Console.WriteLine("Specific command help not found. Available commands:");
            foreach (var cmd in cli.GetAllCommands())
            {
                Console.WriteLine($"- {cmd.Name}"); // Assuming ICommand has a Name property
            }
        }
    }
}
```

## Notes

*   **Thread Safety**: The `RegisterCommand` method modifies the internal collection of commands. If commands are registered dynamically while `ExecuteAsync` or `GetAllCommands` is being called on a different thread, race conditions may occur. It is recommended to perform all registrations during the application startup phase before invoking execution methods.
*   **Execution State**: The `Success` and `Message` properties appear to be stateful members of the `CommandLineInterface` or the returned `CommandResult`. When using `ExecuteAsync`, always rely on the returned `CommandResult` object for the specific outcome of that invocation rather than relying on persistent state if the instance is reused concurrently.
*   **Command Uniqueness**: While the signature of `RegisterCommand` does not explicitly define collision behavior, typical implementations will fail if duplicate command names are registered. Ensure command identifiers are unique prior to registration to avoid runtime exceptions.
*   **Asynchronous Execution**: Since `ExecuteAsync` returns a `Task<CommandResult>`, callers must await the operation. Blocking on this task (e.g., using `.Result` or `.Wait()`) in a single-threaded synchronization context (such as a UI thread or specific ASP.NET contexts) may lead to deadlocks.
