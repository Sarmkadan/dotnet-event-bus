#nullable enable

using System;
using System.Threading.Tasks;
using DotnetEventBus;
using Microsoft.Extensions.DependencyInjection;

// Example 1: Basic v2.0 Features - Event Replay and Audit Logging
namespace DotnetEventBus.Examples.V2BasicUsage
{
	/// <summary>
	/// Represents an event that is raised when a new order is created in the system.
	/// This event carries the essential order information needed for downstream processing.
	/// </summary>
	public sealed class OrderCreatedEvent : IEvent
	{
		/// <summary>
		/// Gets the unique identifier for this event instance.
		/// Generated using Guid.NewGuid() to ensure each event has a distinct identifier.
		/// </summary>
		public string EventId { get; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets the timestamp when this event was created.
		/// Set to DateTime.UtcNow to record when the order was created.
		/// </summary>
		public DateTime Timestamp { get; } = DateTime.UtcNow;

		/// <summary>
		/// Gets the type identifier for this event class.
		/// Returns the class name as a string for event type identification.
		/// </summary>
		public string EventType => nameof(OrderCreatedEvent);

		/// <summary>
		/// Gets or sets the unique identifier for the order.
		/// This is the primary key that links this event to a specific order in the system.
		/// </summary>
		public string OrderId { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for the customer who placed the order.
		/// Used to associate the order with customer records for billing and notifications.
		/// </summary>
		public string CustomerId { get; set; }

		/// <summary>
		/// Gets or sets the total monetary amount of the order.
		/// Represents the financial value of the order in decimal format.
		/// </summary>
		public decimal Amount { get; set; }

		/// <summary>
		/// Gets or sets the name of the product being ordered.
		/// Provides a human-readable description of what product was purchased.
		/// </summary>
		public string ProductName { get; set; }
	}

	/// <summary>
	/// Represents an event that is raised when a payment is processed for an order.
	/// This event carries payment transaction details needed for order fulfillment.
	/// </summary>
	public sealed class PaymentProcessedEvent : IEvent
	{
		/// <summary>
		/// Gets the unique identifier for this event instance.
		/// Generated using Guid.NewGuid() to ensure each event has a distinct identifier.
		/// </summary>
		public string EventId { get; } = Guid.NewGuid().ToString();

		/// <summary>
		/// Gets the timestamp when this event was created.
		/// Set to DateTime.UtcNow to record when the payment was processed.
		/// </summary>
		public DateTime Timestamp { get; } = DateTime.UtcNow;


		/// <summary>
		/// Gets the type identifier for this event class.
		/// Returns the class name as a string for event type identification.
		/// </summary>
		public string EventType => nameof(PaymentProcessedEvent);

		/// <summary>
		/// Gets or sets the unique transaction identifier for the payment.
		/// This is the primary key that links this event to a specific payment transaction in the system.
		/// </summary>
		public string TransactionId { get; set; }

		/// <summary>
		/// Gets or sets the unique identifier for the order.
		/// This is the primary key that links this event to a specific order in the system.
		/// </summary>
		public string OrderId { get; set; }

		/// <summary>
		/// Gets or sets whether the payment was successful.
		/// Indicates whether the payment processing completed successfully or failed.
		/// </summary>
		public bool IsSuccessful { get; set; }
	}

	public sealed class OrderCreatedHandler : EventHandlerBase<OrderCreatedEvent>
	{
		public override async Task Handle(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
		{
			Console.WriteLine($"Processing order {@event.OrderId} for customer {@event.CustomerId}");
			await Task.Delay(10); // Simulate work
			Console.WriteLine($"Order processed: {@event.ProductName} - ${@event.Amount}");
		}
	}

	public sealed class PaymentProcessedHandler : EventHandlerBase<PaymentProcessedEvent>
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
