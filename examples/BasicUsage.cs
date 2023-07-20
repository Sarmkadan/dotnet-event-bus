using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetEventBus.Services;
using DotnetEventBus.Handlers;

// Define a simple event
/// <summary>
/// Represents an event that is published when a new user registers in the system.
/// </summary>
/// <param name="Username">The username of the newly registered user.</param>
/// <param name="Email">The email address of the newly registered user.</param>
public record UserRegisteredEvent(string Username, string Email);

public class BasicUsageExample
{
    public async Task RunAsync()
    {
        // 1. Initialize the Event Bus (In a real app, this is done via DI)
        // For this example, we'll assume we have a way to get an IEventBus instance.
        // IEventBus bus = ...; 

        // 2. Subscribe to an event
        var subscription = bus.Subscribe<UserRegisteredEvent>(async (e, ct) =>
        {
            Console.WriteLine($"User registered: {e.Username} ({e.Email})");
            await Task.CompletedTask;
        });

        // 3. Publish an event
        await bus.PublishAsync(new UserRegisteredEvent("vladyslav", "vladyslav@example.com"));

        // 4. Clean up subscription
        subscription.Dispose();
    }
}
