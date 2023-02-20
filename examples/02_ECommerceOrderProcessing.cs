// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Examples;

/// <summary>
/// E-Commerce Order Processing: Complex workflow with multiple handlers and priorities.
/// Demonstrates real-world scenario with inventory, payment, and notification handling.
/// </summary>
public static class ECommerceOrderProcessingExample
{
    // Event definitions
    public class OrderPlacedEvent
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime PlacedAt { get; set; }
    }

    public class OrderItem
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class PaymentProcessedEvent
    {
        public string OrderId { get; set; }
        public string TransactionId { get; set; }
        public bool IsSuccessful { get; set; }
        public decimal Amount { get; set; }
    }

    public class ShipmentCreatedEvent
    {
        public string OrderId { get; set; }
        public string ShipmentId { get; set; }
        public DateTime EstimatedDelivery { get; set; }
    }

    // Handler 1: High priority - Validate and reserve inventory
    public class InventoryReservationHandler : EventHandlerBase<OrderPlacedEvent>
    {
        public override async Task Handle(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"🏪 [Priority 100] Reserving inventory for order {0}", @event.OrderId);

            foreach (var item in @event.Items)
            {
                Console.WriteLine($"   - Product {0}: {1} units", item.ProductId, item.Quantity);
                await Task.Delay(50); // Simulate inventory check
            }

            Console.WriteLine($"✓ Inventory reserved successfully");
        }
    }

    // Handler 2: Process payment
    public class PaymentProcessingHandler : EventHandlerBase<OrderPlacedEvent>
    {
        private readonly IEventBus _eventBus;

        public PaymentProcessingHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public override async Task Handle(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"💳 [Priority 50] Processing payment for order {0} (${1:F2})",
                @event.OrderId, @event.TotalPrice);

            // Simulate payment processing
            await Task.Delay(200);

            var paymentEvent = new PaymentProcessedEvent
            {
                OrderId = @event.OrderId,
                TransactionId = $"TXN-{Guid.NewGuid().ToString().Substring(0, 8)}",
                IsSuccessful = true,
                Amount = @event.TotalPrice
            };

            await _eventBus.PublishAsync(paymentEvent, cancellationToken);
            Console.WriteLine($"✓ Payment processed: {paymentEvent.TransactionId}");
        }
    }

    // Handler 3: Create shipment (triggered by payment event)
    public class ShipmentCreationHandler : EventHandlerBase<PaymentProcessedEvent>
    {
        private readonly IEventBus _eventBus;

        public ShipmentCreationHandler(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public override async Task Handle(PaymentProcessedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"📦 Creating shipment for order {0}", @event.OrderId);

            await Task.Delay(100); // Simulate shipment creation

            var shipmentEvent = new ShipmentCreatedEvent
            {
                OrderId = @event.OrderId,
                ShipmentId = $"SHIP-{Guid.NewGuid().ToString().Substring(0, 8)}",
                EstimatedDelivery = DateTime.UtcNow.AddDays(3)
            };

            await _eventBus.PublishAsync(shipmentEvent, cancellationToken);
            Console.WriteLine($"✓ Shipment created: {shipmentEvent.ShipmentId}");
        }
    }

    // Handler 4: Send customer notifications (low priority)
    public class CustomerNotificationHandler : EventHandlerBase<OrderPlacedEvent>
    {
        public override async Task Handle(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"📧 [Priority 10] Sending order confirmation to customer {0}", @event.CustomerId);
            await Task.Delay(75);
            Console.WriteLine($"✓ Order confirmation email sent");
        }
    }

    // Handler 5: Shipment notification
    public class ShipmentNotificationHandler : EventHandlerBase<ShipmentCreatedEvent>
    {
        public override async Task Handle(ShipmentCreatedEvent @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"📧 Sending shipping notification for order {0}", @event.OrderId);
            Console.WriteLine($"   Estimated delivery: {0:d}", @event.EstimatedDelivery);
            await Task.Delay(50);
            Console.WriteLine($"✓ Shipping notification sent");
        }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: E-Commerce Order Processing ===\n");

        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = false; // Sequential for clear output
            options.MaxRetryAttempts = 3;
            options.EnableDeadLetterQueue = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        // Create sample order
        var order = new OrderPlacedEvent
        {
            OrderId = "ORD-2026-001",
            CustomerId = "CUST-123",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = "PROD-001", Quantity = 2, UnitPrice = 49.99m },
                new OrderItem { ProductId = "PROD-002", Quantity = 1, UnitPrice = 99.99m }
            },
            TotalPrice = 199.97m,
            PlacedAt = DateTime.UtcNow
        };

        Console.WriteLine($"Order Details:");
        Console.WriteLine($"  ID: {order.OrderId}");
        Console.WriteLine($"  Customer: {order.CustomerId}");
        Console.WriteLine($"  Total: ${order.TotalPrice:F2}");
        Console.WriteLine($"  Items: {order.Items.Count}\n");

        // Publish the order
        Console.WriteLine("--- Publishing OrderPlacedEvent ---\n");
        var result = await eventBus.PublishAsync(order);

        Console.WriteLine($"\n✓ Order processing completed:");
        Console.WriteLine($"  - Total events published: 2");
        Console.WriteLine($"  - Total handlers invoked: {result.HandlersInvoked}");
        Console.WriteLine($"  - Total duration: {result.Duration.TotalMilliseconds:F2}ms");

        Console.WriteLine("\n=== Example completed successfully ===");
    }
}
