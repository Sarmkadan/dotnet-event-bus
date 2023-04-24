#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Examples;

/// <summary>
/// Basic Pub-Sub example: Publishing events and handling them with multiple subscribers.
/// </summary>
public static class BasicPubSubExample
{
    // Event definition
    public sealed class UserRegisteredEvent
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    // Handler 1: Send welcome email
    public sealed class SendWelcomeEmailHandler : EventHandlerBase<UserRegisteredEvent>
    {
        public override async Task Handle(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"📧 Sending welcome email to {0}", @event.Email);
            await Task.Delay(100); // Simulate email sending
            Console.WriteLine($"✓ Welcome email sent to {0}", @event.Email);
        }
    }

    // Handler 2: Update user profile
    public sealed class UpdateUserProfileHandler : EventHandlerBase<UserRegisteredEvent>
    {
        public override async Task Handle(UserRegisteredEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"👤 Updating user profile for {0}", @event.FullName);
            await Task.Delay(50); // Simulate DB update
            Console.WriteLine($"✓ User profile updated");
        }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: Basic Pub-Sub Example ===\n");

        // Setup DI
        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = true;
            options.MaxConcurrentHandlers = 4;
            options.EnableDetailedLogging = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        // Subscribe handlers using delegate syntax
        eventBus.Subscribe<UserRegisteredEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"📊 Recording user registration in analytics");
                await Task.Delay(75);
                Console.WriteLine($"✓ Analytics recorded");
            },
            handlerName: "AnalyticsHandler",
            priority: 0
        );

        // Publish multiple events
        var users = new[]
        {
            new UserRegisteredEvent
            {
                UserId = "user-001",
                Email = "alice@example.com",
                FullName = "Alice Johnson",
                RegisteredAt = DateTime.UtcNow
            },
            new UserRegisteredEvent
            {
                UserId = "user-002",
                Email = "bob@example.com",
                FullName = "Bob Smith",
                RegisteredAt = DateTime.UtcNow
            },
            new UserRegisteredEvent
            {
                UserId = "user-003",
                Email = "charlie@example.com",
                FullName = "Charlie Brown",
                RegisteredAt = DateTime.UtcNow
            }
        };

        foreach (var user in users)
        {
            Console.WriteLine($"\n--- Publishing event for {user.FullName} ---");
            var result = await eventBus.PublishAsync(user);

            Console.WriteLine($"✓ Event published:");
            Console.WriteLine($"  - Handlers invoked: {result.HandlersInvoked}");
            Console.WriteLine($"  - Duration: {result.Duration.TotalMilliseconds:F2}ms");
        }

        Console.WriteLine("\n=== Example completed successfully ===");
    }
}
