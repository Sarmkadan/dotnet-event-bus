# BasicPubSubExample
BasicPubSubExample is a simple demonstration class that models a user entity and shows how to handle domain events using the event‑bus infrastructure in the dotnet-event-bus project. It exposes user‑related data as public fields and provides overridden asynchronous handlers for specific event types, as well as a static entry‑point method for running a console‑based publish/subscribe scenario.

## API
### UserId
`public string UserId`  
Stores the unique identifier for the user.  
- **Purpose:** Holds the user’s ID value.  
- **Parameters:** None (property).  
- **Return value:** The current string value; `null` if not set.  
- **When it throws:** No exceptions are thrown by the getter or setter; assigning `null` is allowed and will store a null reference.

### Email
`public string Email`  
Stores the user’s e‑mail address.  
- **Purpose:** Holds the user’s e‑mail.  
- **Parameters:** None (property).  
- **Return value:** The current string value; `null` if not set.  
- **When it throws:** No exceptions are thrown by the getter or setter; assigning `null` is allowed.

### FullName
`public string FullName`  
Stores the user’s full name.  
- **Purpose:** Holds the user’s full name.  
- **Parameters:** None (property).  
- **Return value:** The current string value; `null` if not set.  
- **When it throws:** No exceptions are thrown by the getter or setter; assigning `null` is allowed.

### RegisteredAt
`public DateTime RegisteredAt`  
Stores the date and time when the user was registered.  
- **Purpose:** Holds the registration timestamp.  
- **Parameters:** None (property).  
- **Return value:** The current DateTime value; defaults to `DateTime.MinValue` if never assigned.  
- **When it throws:** No exceptions are thrown by the getter or setter.

### Handle (first overload)
`public override async Task Handle(TFirstEvent @event, CancellationToken cancellationToken = default)`  
Asynchronously processes a `TFirstEvent` (e.g., a user‑registration event).  
- **Purpose:** Updates the instance’s properties based on the event data and performs any side‑effects required by the example.  
- **Parameters:**  
  - `@event`: The event object containing the data to apply. Must not be `null`.  
  - `cancellationToken`: Optional token to observe for cancellation requests.  
- **Return value:** A `Task` that completes when the handling logic finishes.  
- **When it throws:**  
  - `ArgumentNullException` if `@event` is `null`.  
  - Any exception thrown by downstream logic (e.g., I/O operations) will propagate out of the method.

### Handle (second overload)
`public override async Task Handle(TSecondEvent @event, CancellationToken cancellationToken = default)`  
Asynchronously processes a `TSecondEvent` (e.g., a user‑profile‑update event).  
- **Purpose:** Updates the instance’s properties based on the event data and performs any side‑effects required by the example.  
- **Parameters:**  
  - `@event`: The event object containing the data to apply. Must not be `null`.  
  - `cancellationToken`: Optional token to observe for cancellation requests.  
- **Return value:** A `Task` that completes when the handling logic finishes.  
- **When it throws:**  
  - `ArgumentNullException` if `@event` is `null`.  
  - Any exception thrown by downstream logic will propagate out of the method.

### Main
`public static async Task Main(string[] args)`  
Entry point for the console demonstration.  
- **Purpose:** Configures the event bus, subscribes an instance of `BasicPubSubExample` to the relevant event types, publishes a few sample events, and waits for shutdown.  
- **Parameters:**  
  - `args`: Command‑line arguments (unused in the current implementation).  
- **Return value:** A `Task` that completes when the application exits gracefully.  
- **When it throws:**  
  - `InvalidOperationException` if the event bus cannot be initialized (e.g., missing configuration).  
  - Any exception thrown during event publishing or handling will cause the method to fault; the host will typically log the error and exit with a non‑zero status code.

## Usage
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetEventBus.Events;   // Hypothetical namespace for event types
using DotNetEventBus.Hosting; // Hypothetical namespace for bus setup

class Program
{
    static async Task Main(string[] args)
    {
        // Set up the event bus (implementation‑specific)
        var bus = EventBusBuilder.Create()
                                 .AddSubscriber<BasicPubSubExample>()
                                 .Build();

        using (var cts = new CancellationTokenSource())
        {
            // Start the bus
            await bus.StartAsync(cts.Token);

            // Publish a user‑registration event
            await bus.PublishAsync(
                new UserRegisteredEvent
                {
                    UserId = "usr-123",
                    Email = "alice@example.com",
                    FullName = "Alice Engineer",
                    RegisteredAt = DateTime.UtcNow
                },
                cts.Token);

            // Publish a profile‑update event
            await bus.PublishAsync(
                new UserProfileUpdatedEvent
                {
                    UserId = "usr-123",
                    Email = "alice.new@example.com",
                    FullName = "Alice Engineer",
                    RegisteredAt = DateTime.UtcNow.AddDays(-10)
                },
                cts.Token);

            // Wait for user to press Enter to shut down
            Console.WriteLine("Press Enter to exit...");
            await Console.ReadLineAsync();

            // Signal cancellation and stop the bus
            cts.Cancel();
            await bus.StopAsync();
        }
    }
}
```

```csharp
using System;
using System.Threading.Tasks;
using DotNetEventBus.Handlers; // Hypothetical base handler namespace

// Example of manually invoking the handlers without the bus infrastructure
class ManualDemo
{
    static async Task()
    {
        var handler = new BasicPubSubExample();

        var regEvent = new UserRegisteredEvent
        {
            UserId = "usr-456",
            Email = "bob@example.com",
            FullName = "Bob Builder",
            RegisteredAt = DateTime.UtcNow
        };

        await handler.Handle(regEvent); // First overload

        Console.WriteLine($"After registration: {handler.FullName} ({handler.Email})");

        var updEvent = new UserProfileUpdatedEvent
        {
            UserId = "usr-456",
            Email = "bob.new@example.com",
            FullName = "Bob Builder",
            RegisteredAt = DateTime.UtcNow
        };

        await handler.Handle(updEvent); // Second overload

        Console.WriteLine($"After update: {handler.FullName} ({handler.Email})");
    }
}
```

## Notes
- The `UserId`, `Email`, `FullName`, and `RegisteredAt` members are simple fields/properties with no built‑in validation; callers must ensure that values conform to domain requirements before assignment.  
- Both `Handle` overloads are **not thread‑safe** for concurrent invocations on the same instance; if multiple threads may call `Handle` simultaneously, external synchronization (e.g., locking) is required.  
- The `Main` method assumes a console host; when hosted in other environments (e.g., ASP.NET Core), the static entry point should be replaced with appropriate service registration.  
- If a `null` event is passed to either `Handle` overload, an `ArgumentNullException` is thrown immediately; the method does not attempt to recover from such misuse.  
- The `RegisteredAt` property defaults to `DateTime.MinValue` if never set; relying on this default may produce misleading timestamps in downstream logic.  
- Cancellation tokens passed to the `Handle` methods are respected only if the implementation contains awaitable operations that observe the token; otherwise, the token may be ignored.  
- The static `Main` method returns a `Task` to enable asynchronous top‑level entry points; callers should await it or use `.GetAwaiter().GetResult()` in synchronous contexts.
