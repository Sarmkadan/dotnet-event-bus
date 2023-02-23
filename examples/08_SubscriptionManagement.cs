// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Examples;

/// <summary>
/// Subscription Management: Demonstrates runtime subscription management,
/// handler enabling/disabling, and subscription statistics.
/// </summary>
public static class SubscriptionManagementExample
{
    public class UserActionEvent
    {
        public string UserId { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: Subscription Management ===\n");

        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = false;
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        var subscriptionManager = serviceProvider.GetRequiredService<ISubscriptionManager>();

        // Register multiple handlers
        Console.WriteLine("--- Registering Event Handlers ---\n");

        eventBus.Subscribe<UserActionEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  📧 EmailNotificationHandler: Sending email for {@event.Action}");
                await Task.Delay(50);
            },
            handlerName: "EmailNotificationHandler",
            priority: 10
        );

        eventBus.Subscribe<UserActionEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  💾 DatabaseLogger: Recording action in database");
                await Task.Delay(30);
            },
            handlerName: "DatabaseLogger",
            priority: 5
        );

        eventBus.Subscribe<UserActionEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  📊 AnalyticsProcessor: Processing analytics");
                await Task.Delay(40);
            },
            handlerName: "AnalyticsProcessor",
            priority: 1
        );

        eventBus.Subscribe<UserActionEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  🔔 PushNotification: Sending push notification");
                await Task.Delay(25);
            },
            handlerName: "PushNotificationHandler",
            priority: 8
        );

        // List subscriptions
        await DisplaySubscriptions(subscriptionManager);

        // Publish events with all handlers
        Console.WriteLine("\n--- Publishing Event (All Handlers Enabled) ---\n");
        var eventBefore = new UserActionEvent
        {
            UserId = "user-001",
            Action = "Login",
            Timestamp = DateTime.UtcNow
        };
        await eventBus.PublishAsync(eventBefore);

        // Disable a handler
        Console.WriteLine("\n--- Disabling EmailNotificationHandler ---\n");
        await subscriptionManager.DisableHandlerAsync("EmailNotificationHandler");
        Console.WriteLine("✓ Handler disabled\n");

        // Publish with disabled handler
        Console.WriteLine("--- Publishing Event (Email Disabled) ---\n");
        var eventAfter = new UserActionEvent
        {
            UserId = "user-002",
            Action = "Purchase",
            Timestamp = DateTime.UtcNow
        };
        await eventBus.PublishAsync(eventAfter);

        // Get handler statistics
        Console.WriteLine("\n--- Handler Statistics ---\n");
        var stats = await subscriptionManager.GetStatisticsAsync();

        foreach (var (handlerName, handlerStats) in stats)
        {
            Console.WriteLine($"Handler: {handlerName}");
            Console.WriteLine($"  Invocations: {handlerStats.InvocationCount}");
            Console.WriteLine($"  Success: {handlerStats.SuccessCount}");
            Console.WriteLine($"  Failures: {handlerStats.FailureCount}");

            if (handlerStats.InvocationCount > 0)
            {
                Console.WriteLine($"  Avg Duration: {handlerStats.AverageDuration:F2}ms");
                Console.WriteLine($"  Min Duration: {handlerStats.MinDuration:F2}ms");
                Console.WriteLine($"  Max Duration: {handlerStats.MaxDuration:F2}ms");
            }

            Console.WriteLine();
        }

        // Re-enable handler
        Console.WriteLine("--- Re-enabling EmailNotificationHandler ---\n");
        await subscriptionManager.EnableHandlerAsync("EmailNotificationHandler");
        Console.WriteLine("✓ Handler enabled\n");

        // Publish with re-enabled handler
        Console.WriteLine("--- Publishing Event (Email Re-enabled) ---\n");
        var eventReenabled = new UserActionEvent
        {
            UserId = "user-003",
            Action = "Profile Update",
            Timestamp = DateTime.UtcNow
        };
        await eventBus.PublishAsync(eventReenabled);

        // Final subscription status
        await DisplaySubscriptions(subscriptionManager);

        // Demonstrate unsubscribing
        Console.WriteLine("\n--- Unsubscribing AnalyticsProcessor ---\n");
        await eventBus.UnsubscribeAsync("AnalyticsProcessor");
        Console.WriteLine("✓ Unsubscribed\n");

        // List final subscriptions
        await DisplaySubscriptions(subscriptionManager);

        Console.WriteLine("=== Example completed successfully ===");
    }

    private static async Task DisplaySubscriptions(ISubscriptionManager subscriptionManager)
    {
        Console.WriteLine("--- Current Subscriptions ---\n");

        var subscriptions = await subscriptionManager.GetSubscriptionsAsync(nameof(UserActionEvent));

        if (!subscriptions.Any())
        {
            Console.WriteLine("No subscriptions registered.\n");
            return;
        }

        Console.WriteLine($"Total subscriptions: {subscriptions.Count}\n");

        // Display sorted by priority
        foreach (var sub in subscriptions.OrderByDescending(s => s.Priority))
        {
            var statusIcon = sub.IsEnabled ? "✓" : "✗";
            var status = sub.IsEnabled ? "Enabled" : "Disabled";
            Console.WriteLine($"{statusIcon} {sub.HandlerName}");
            Console.WriteLine($"   Priority: {sub.Priority}");
            Console.WriteLine($"   Status: {status}");
            Console.WriteLine($"   Registered: {sub.RegisteredAt:u}\n");
        }
    }
}
