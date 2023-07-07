# SubscriptionManagementExample

The `SubscriptionManagementExample` class serves as a minimal demonstration of recording a subscription management action within the `dotnet-event-bus` project. It captures the user performing the action, the type of action (e.g., subscribe, unsubscribe), and the exact time the action occurred. The class also includes a static `Main` method that can be used as an entry point to illustrate basic usage.

## API

### `UserId` (string)

Gets or sets the identifier of the user who performed the subscription action.  
- **Purpose**: Uniquely identifies the user associated with the subscription change.  
- **Parameters**: None (property).  
- **Return value**: The current user ID string.  
- **Throws**: No direct exceptions; however, setting a `null` value is permitted and may lead to `NullReferenceException` if consumed elsewhere without a null check.

### `Action` (string)

Gets or sets the description of the subscription management action.  
- **Purpose**: Indicates what action was taken, such as `"subscribe"`, `"unsubscribe"`, or `"update"`.  
- **Parameters**: None (property).  
- **Return value**: The current action string.  
- **Throws**: No direct exceptions; setting an empty or `null` value is allowed but may cause logical errors in downstream processing.

### `Timestamp` (DateTime)

Gets or sets the date and time when the subscription action occurred.  
- **Purpose**: Provides a precise timestamp for auditing or ordering events.  
- **Parameters**: None (property).  
- **Return value**: The current `DateTime` value.  
- **Throws**: No direct exceptions; the default value (`DateTime.MinValue`) is valid but may indicate an uninitialized state.

### `Main` (static async Task)

The application entry point.  
- **Purpose**: Demonstrates the creation and use of a `SubscriptionManagementExample` instance.  
- **Parameters**: None (the signature `public static async Task Main` does not accept command-line arguments).  
- **Return value**: A `Task` representing the asynchronous operation.  
- **Throws**: May throw exceptions from any asynchronous operations performed inside the method (e.g., network calls, file I/O). The exact exceptions depend on the implementation.

## Usage

### Example 1: Basic property assignment and output

```csharp
using System;

public class Program
{
    public static async Task Main()
    {
        var example = new SubscriptionManagementExample
        {
            UserId = "user-42",
            Action = "subscribe",
            Timestamp = DateTime.UtcNow
        };

        Console.WriteLine($"User {example.UserId} performed '{example.Action}' at {example.Timestamp:O}");
        await Task.CompletedTask; // Simulate async work
    }
}
```

### Example 2: Simulating an event bus subscription handler

```csharp
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main()
    {
        // Simulate receiving a subscription event
        var subscriptionEvent = new SubscriptionManagementExample
        {
            UserId = "alice@example.com",
            Action = "unsubscribe",
            Timestamp = DateTime.UtcNow
        };

        // Process the event asynchronously (e.g., log to a database)
        await ProcessSubscriptionAsync(subscriptionEvent);
    }

    private static async Task ProcessSubscriptionAsync(SubscriptionManagementExample sub)
    {
        // Simulate async I/O
        await Task.Delay(100);
        Console.WriteLine($"Processed {sub.Action} for {sub.UserId} at {sub.Timestamp}");
    }
}
```

## Notes

- **Edge cases**:  
  - `UserId` and `Action` can be set to `null` or empty strings. Downstream code should validate these values before use to avoid `NullReferenceException` or unintended behavior.  
  - `Timestamp` defaults to `DateTime.MinValue` if not explicitly assigned. This value may represent an invalid or uninitialized state and should be checked before relying on it for ordering or display.  
  - The `Main` method signature omits command-line arguments (`string[] args`). If argument parsing is required, the method must be overloaded or the arguments must be obtained through other means (e.g., `Environment.GetCommandLineArgs()`).

- **Thread safety**:  
  - Instance properties (`UserId`, `Action`, `Timestamp`) are not thread-safe. Concurrent reads and writes from multiple threads may result in inconsistent state. Synchronization (e.g., locks or immutable design) is recommended if the object is shared across threads.  
  - The static `Main` method runs on the main thread; any asynchronous operations inside it should be awaited properly to avoid unobserved exceptions.
