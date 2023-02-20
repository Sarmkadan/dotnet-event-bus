// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Examples;

/// <summary>
/// Dead Letter Queue Handling: Demonstrates error handling, retries, and recovery.
/// Shows how the event bus manages failed events and provides recovery mechanisms.
/// </summary>
public static class DeadLetterQueueHandlingExample
{
    public class PaymentProcessingEvent
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class NotificationEvent
    {
        public string RecipientId { get; set; }
        public string Message { get; set; }
        public string Channel { get; set; } // Email, SMS, Push
    }

    // Handler that sometimes fails (simulates external service failures)
    public class FlakeyPaymentHandler : EventHandlerBase<PaymentProcessingEvent>
    {
        private static int _callCount = 0;

        public override async Task Handle(PaymentProcessingEvent @event, CancellationToken cancellationToken = default)
        {
            _callCount++;
            Console.WriteLine($"💳 Processing payment (attempt {_callCount}): Order {0}, Amount: ${1:F2}",
                @event.OrderId, @event.Amount);

            await Task.Delay(100);

            // Simulate transient failure (network timeout)
            if (_callCount % 2 == 0)
                throw new TimeoutException("Payment gateway timeout");

            Console.WriteLine($"✓ Payment processed successfully");
        }
    }

    // Handler that logs failures
    public class NotificationHandler : EventHandlerBase<NotificationEvent>
    {
        private static int _notificationCount = 0;

        public override async Task Handle(NotificationEvent @event, CancellationToken cancellationToken = default)
        {
            _notificationCount++;
            Console.WriteLine($"📧 Sending {0} notification to {1}", @event.Channel, @event.RecipientId);

            await Task.Delay(50);

            // Simulate periodic failures
            if (_notificationCount % 3 == 0)
                throw new InvalidOperationException("Notification service unavailable");

            Console.WriteLine($"✓ Notification sent via {0}", @event.Channel);
        }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: Dead Letter Queue Handling ===\n");

        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.MaxRetryAttempts = 3;
            options.RetryDelayMultiplier = 2.0;
            options.InitialRetryDelayMs = 100;
            options.EnableDeadLetterQueue = true;
            options.AllowParallelHandling = false;
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();
        var dlqService = serviceProvider.GetRequiredService<IDeadLetterService>();

        // Register handlers
        Console.WriteLine("Registering event handlers...\n");

        eventBus.Subscribe<NotificationEvent>(
            async (@event, ct) => await new NotificationHandler().Handle(@event, ct),
            handlerName: "NotificationHandler"
        );

        // Publish events that will fail
        Console.WriteLine("--- Publishing Events ---\n");

        var paymentEvents = new[]
        {
            new PaymentProcessingEvent { OrderId = "ORD-001", Amount = 99.99m, PaymentMethod = "Credit Card" },
            new PaymentProcessingEvent { OrderId = "ORD-002", Amount = 199.99m, PaymentMethod = "Debit Card" },
            new PaymentProcessingEvent { OrderId = "ORD-003", Amount = 299.99m, PaymentMethod = "PayPal" }
        };

        foreach (var payment in paymentEvents)
        {
            var result = await eventBus.PublishAsync(payment);
            Console.WriteLine($"Publish result: {result.HandlersInvoked} handlers invoked, " +
                $"{result.HandlersFailed} failed\n");
        }

        var notificationEvents = new[]
        {
            new NotificationEvent { RecipientId = "user-1", Message = "Order confirmed", Channel = "Email" },
            new NotificationEvent { RecipientId = "user-2", Message = "Order shipped", Channel = "SMS" },
            new NotificationEvent { RecipientId = "user-3", Message = "Delivery today", Channel = "Push" }
        };

        foreach (var notification in notificationEvents)
        {
            await eventBus.PublishAsync(notification);
        }

        // Check dead letter queue
        Console.WriteLine("\n--- Dead Letter Queue Status ---\n");
        var deadLetterEntries = await dlqService.GetPendingEntriesAsync();

        Console.WriteLine($"Total pending entries: {deadLetterEntries.Count}\n");

        if (deadLetterEntries.Any())
        {
            Console.WriteLine("Pending Failed Events:\n");
            foreach (var entry in deadLetterEntries)
            {
                Console.WriteLine($"ID: {entry.Id}");
                Console.WriteLine($"Event Type: {entry.EventType}");
                Console.WriteLine($"Retry Count: {entry.RetryCount}/{entry.MaxRetries}");
                Console.WriteLine($"Failed At: {entry.FailedAt:u}");
                Console.WriteLine($"Last Error: {entry.LastException?.Message}");
                Console.WriteLine($"Next Retry: {entry.NextRetryAt:u}\n");
            }
        }

        // Get statistics
        var stats = await dlqService.GetStatisticsAsync();
        Console.WriteLine("Dead Letter Queue Statistics:");
        Console.WriteLine($"  - Pending Entries: {stats.PendingEntries}");
        Console.WriteLine($"  - Total Failed: {stats.TotalFailedEntries}");
        Console.WriteLine($"  - Reprocessed: {stats.ReprocessedEntries}");
        if (stats.OldestEntry != default)
            Console.WriteLine($"  - Oldest Entry: {stats.OldestEntry:u}");

        // Demonstrate reprocessing
        Console.WriteLine("\n--- Reprocessing Failed Events ---\n");

        if (deadLetterEntries.Any())
        {
            var entriesToReprocess = deadLetterEntries.Take(2).ToList();
            Console.WriteLine($"Reprocessing {entriesToReprocess.Count} failed events...\n");

            foreach (var entry in entriesToReprocess)
            {
                try
                {
                    Console.WriteLine($"Reprocessing: {entry.EventType} (ID: {entry.Id})");
                    await dlqService.ReprocessEntryAsync(entry.Id);
                    Console.WriteLine($"✓ Reprocessing initiated\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Reprocessing failed: {ex.Message}\n");
                }
            }
        }

        // Check updated statistics
        Console.WriteLine("--- Updated Dead Letter Queue Statistics ---\n");
        var updatedStats = await dlqService.GetStatisticsAsync();
        Console.WriteLine($"Pending entries (after reprocessing): {updatedStats.PendingEntries}");
        Console.WriteLine($"Total reprocessed attempts: {updatedStats.ReprocessedEntries}");

        // Demonstrate deletion of entries
        if (deadLetterEntries.Any())
        {
            var entryToDelete = deadLetterEntries.Last();
            Console.WriteLine($"\n--- Deleting Failed Entry ---\n");
            Console.WriteLine($"Deleting entry: {entryToDelete.Id}");
            await dlqService.DeleteEntryAsync(entryToDelete.Id);
            Console.WriteLine($"✓ Entry deleted");

            var finalStats = await dlqService.GetStatisticsAsync();
            Console.WriteLine($"\nFinal pending entries: {finalStats.PendingEntries}");
        }

        Console.WriteLine("\n=== Example completed successfully ===");
    }
}
