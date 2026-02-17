using System;
using System.Threading.Tasks;
using DotnetEventBus;
using Microsoft.Extensions.DependencyInjection;

// Example 1: Basic v2.0 Features - Event Replay and Audit Logging
namespace DotnetEventBus.Examples.V2BasicUsage
{
    public class OrderCreatedEvent : IEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public string EventType => nameof(OrderCreatedEvent);

        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string ProductName { get; set; }
    }

    public class PaymentProcessedEvent : IEvent
    {
        public string EventId { get; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public string EventType => nameof(PaymentProcessedEvent);

        public string TransactionId { get; set; }
        public string OrderId { get; set; }
        public bool IsSuccessful { get; set; }
    }

    public class OrderCreatedHandler : EventHandlerBase<OrderCreatedEvent>
    {
        public override async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Processing order {@event.OrderId} for customer {@event.CustomerId}");
            await Task.Delay(10); // Simulate work
            Console.WriteLine($"Order processed: {@event.ProductName} - ${@event.Amount}");
        }
    }

    public class PaymentProcessedHandler : EventHandlerBase<PaymentProcessedEvent>
    {
        public override async Task Handle(PaymentProcessedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Payment processed for order {@event.OrderId}, Transaction: {@event.TransactionId}, Success: {@event.IsSuccessful}");
            await Task.Delay(10); // Simulate work
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== DotnetEventBus v2.0 Basic Usage Example ===\n");

            // Setup DI container
            var services = new ServiceCollection();

            // Configure EventBus with v2.0 features
            services.AddEventBus(options =>
            {
                options.EnableEventReplay = true;
                options.EnableAuditLogging = true;
                options.EnableEventSourcing = true;
                options.EnableDeadLetterQueue = true;
                options.MaxRetryAttempts = 3;
                options.AllowParallelHandling = true;
                options.MaxConcurrentHandlers = 4;
            });

            var serviceProvider = services.BuildServiceProvider();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();

            // Register handlers
            var subscriptionManager = serviceProvider.GetRequiredService<ISubscriptionManager>();
            eventBus.Subscribe<OrderCreatedEvent>(async (@event, ct) =>
            {
                Console.WriteLine($"Order processed: {@event.OrderId}");
                await Task.CompletedTask;
            }, "OrderHandler");

            eventBus.Subscribe<PaymentProcessedEvent>(async (@event, ct) =>
            {
                Console.WriteLine($"Payment processed: {@event.OrderId}");
                await Task.CompletedTask;
            }, "PaymentHandler");

            // Publish some events
            Console.WriteLine("Publishing events...");
            await eventBus.PublishAsync(new OrderCreatedEvent
            {
                OrderId = "ORD-001",
                CustomerId = "CUST-123",
                Amount = 299.99m,
                ProductName = "Gaming Laptop"
            });

            await eventBus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = "ORD-001",
                TransactionId = "TX-789",
                IsSuccessful = true
            });

            // Demonstrate Event Replay
            Console.WriteLine("\n--- Event Replay Demonstration ---");
            await DemonstrateEventReplay(serviceProvider);

            // Demonstrate Audit Logging
            Console.WriteLine("\n--- Audit Log Demonstration ---");
            await DemonstrateAuditLogging(serviceProvider);

            Console.WriteLine("\nExample completed successfully!");
        }

        static async Task DemonstrateEventReplay(IServiceProvider serviceProvider)
        {
            var replayer = serviceProvider.GetRequiredService<IEventReplayer>();
            var result = await replayer.ReplayAsync(
                from: DateTime.UtcNow.AddMinutes(-5),
                to: DateTime.UtcNow
            );

            Console.WriteLine($"Replayed {result.TotalEvents} events in {result.Duration.TotalMilliseconds}ms");
        }

        static async Task DemonstrateAuditLogging(IServiceProvider serviceProvider)
        {
            var metrics = serviceProvider.GetRequiredService<IMetricsCollector>();
            var systemMetrics = metrics.GetSystemMetrics();
            Console.WriteLine($"Total Events Published: {systemMetrics.TotalEventsPublished}");
            Console.WriteLine($"Success Rate: {systemMetrics.SuccessRate:P2}");
        }
    }
}