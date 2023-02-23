// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;
using DotnetEventBus.Handlers;

namespace DotnetEventBus.Examples;

/// <summary>
/// Event Filtering: Demonstrates selective event handler execution based on
/// event properties using fluent filter APIs.
/// </summary>
public static class EventFilteringExample
{
    public class SalesEvent
    {
        public string OrderId { get; set; }
        public string Region { get; set; }
        public decimal Amount { get; set; }
        public string CustomerSegment { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AlertEvent
    {
        public string AlertId { get; set; }
        public string Severity { get; set; } // Critical, High, Medium, Low
        public string Source { get; set; }
        public string Message { get; set; }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== DotnetEventBus: Event Filtering ===\n");

        var services = new ServiceCollection();
        services.AddEventBus(options =>
        {
            options.AllowParallelHandling = false;
        });

        var serviceProvider = services.BuildServiceProvider();
        var eventBus = serviceProvider.GetRequiredService<IEventBus>();

        // Setup filtered handlers
        SetupSalesHandlers(eventBus);
        SetupAlertHandlers(eventBus);

        // Test with various sales events
        Console.WriteLine("--- Publishing Sales Events ---\n");

        var salesEvents = new[]
        {
            new SalesEvent
            {
                OrderId = "ORD-001",
                Region = "North America",
                Amount = 500m,
                CustomerSegment = "Premium",
                Timestamp = DateTime.UtcNow
            },
            new SalesEvent
            {
                OrderId = "ORD-002",
                Region = "Europe",
                Amount = 150m,
                CustomerSegment = "Standard",
                Timestamp = DateTime.UtcNow
            },
            new SalesEvent
            {
                OrderId = "ORD-003",
                Region = "Asia",
                Amount = 5000m,
                CustomerSegment = "Enterprise",
                Timestamp = DateTime.UtcNow
            },
            new SalesEvent
            {
                OrderId = "ORD-004",
                Region = "North America",
                Amount = 2500m,
                CustomerSegment = "Premium",
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var salesEvent in salesEvents)
        {
            Console.WriteLine($"Publishing order: {salesEvent.OrderId} (${salesEvent.Amount:F2}, {salesEvent.CustomerSegment})");
            await eventBus.PublishAsync(salesEvent);
            Console.WriteLine();
        }

        // Test with various alert events
        Console.WriteLine("\n--- Publishing Alert Events ---\n");

        var alertEvents = new[]
        {
            new AlertEvent
            {
                AlertId = "ALT-001",
                Severity = "Critical",
                Source = "Database",
                Message = "Connection pool exhausted"
            },
            new AlertEvent
            {
                AlertId = "ALT-002",
                Severity = "Low",
                Source = "API",
                Message = "Response time slightly elevated"
            },
            new AlertEvent
            {
                AlertId = "ALT-003",
                Severity = "High",
                Source = "Memory",
                Message = "Heap usage exceeds 80%"
            },
            new AlertEvent
            {
                AlertId = "ALT-004",
                Severity = "Critical",
                Source = "Disk",
                Message = "Disk space critically low"
            }
        };

        foreach (var alertEvent in alertEvents)
        {
            Console.WriteLine($"Publishing alert: {alertEvent.AlertId} ({alertEvent.Severity})");
            await eventBus.PublishAsync(alertEvent);
            Console.WriteLine();
        }

        Console.WriteLine("=== Example completed successfully ===");
    }

    private static void SetupSalesHandlers(IEventBus eventBus)
    {
        // Filter 1: High-value orders (> $1000)
        var highValueFilter = new EventFilterBuilder()
            .Where<SalesEvent>(e => e.Amount > 1000m)
            .Build();

        eventBus.Subscribe<SalesEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  💰 [HighValueHandler] Processing high-value order: ${@event.Amount:F2}");
                await Task.Delay(50);
                Console.WriteLine($"      ✓ Alert sent to special handling team");
            },
            handlerName: "HighValueOrderHandler",
            filter: highValueFilter
        );

        // Filter 2: Premium customers
        var premiumFilter = new EventFilterBuilder()
            .Where<SalesEvent>(e => e.CustomerSegment == "Premium")
            .Build();

        eventBus.Subscribe<SalesEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  🎁 [PremiumHandler] Offering loyalty rewards for {0}", @event.CustomerSegment);
                await Task.Delay(30);
                Console.WriteLine($"      ✓ Loyalty points added");
            },
            handlerName: "PremiumCustomerHandler",
            filter: premiumFilter
        );

        // Filter 3: North America region
        var northAmericaFilter = new EventFilterBuilder()
            .Where<SalesEvent>(e => e.Region == "North America")
            .Build();

        eventBus.Subscribe<SalesEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  📍 [RegionalHandler] Processing North America order");
                await Task.Delay(40);
                Console.WriteLine($"      ✓ Added to regional fulfillment queue");
            },
            handlerName: "NorthAmericaRegionalHandler",
            filter: northAmericaFilter
        );

        // Filter 4: Enterprise segment (exclusive expensive orders)
        var enterpriseHighValueFilter = new EventFilterBuilder()
            .Where<SalesEvent>(e => e.CustomerSegment == "Enterprise")
            .And(e => e.Amount > 2000m)
            .Build();

        eventBus.Subscribe<SalesEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  👔 [EnterpriseHandler] Executive review for enterprise order");
                await Task.Delay(60);
                Console.WriteLine($"      ✓ Assigned to account manager");
            },
            handlerName: "EnterpriseAccountHandler",
            filter: enterpriseHighValueFilter
        );

        // No filter: all sales events
        eventBus.Subscribe<SalesEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  📊 [AnalyticsHandler] Recording sale in analytics");
                await Task.Delay(20);
                Console.WriteLine($"      ✓ Analytics recorded");
            },
            handlerName: "SalesAnalyticsHandler"
        );
    }

    private static void SetupAlertHandlers(IEventBus eventBus)
    {
        // Filter 1: Critical and High severity
        var criticalFilter = new EventFilterBuilder()
            .Where<AlertEvent>(e => e.Severity == "Critical" || e.Severity == "High")
            .Build();

        eventBus.Subscribe<AlertEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  🚨 [CriticalAlertHandler] CRITICAL ALERT!");
                Console.WriteLine($"      Source: {0}, Message: {1}", @event.Source, @event.Message);
                await Task.Delay(100);
                Console.WriteLine($"      ✓ Incident ticket created");
                Console.WriteLine($"      ✓ On-call engineer notified");
            },
            handlerName: "CriticalAlertHandler",
            filter: criticalFilter
        );

        // Filter 2: Database alerts only
        var dbAlertFilter = new EventFilterBuilder()
            .Where<AlertEvent>(e => e.Source == "Database")
            .Build();

        eventBus.Subscribe<AlertEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  🗄️ [DbAlertHandler] Database issue detected");
                await Task.Delay(50);
                Console.WriteLine($"      ✓ Database team notified");
            },
            handlerName: "DatabaseAlertHandler",
            filter: dbAlertFilter
        );

        // Filter 3: Low severity info only
        var infoFilter = new EventFilterBuilder()
            .Where<AlertEvent>(e => e.Severity == "Low")
            .Build();

        eventBus.Subscribe<AlertEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  ℹ️ [InfoHandler] Logging informational alert");
                await Task.Delay(20);
                Console.WriteLine($"      ✓ Added to monitoring dashboard");
            },
            handlerName: "InfoAlertHandler",
            filter: infoFilter
        );

        // No filter: all alerts
        eventBus.Subscribe<AlertEvent>(
            async (@event, ct) =>
            {
                Console.WriteLine($"  📝 [AuditHandler] Audit logging all alerts");
                await Task.Delay(10);
                Console.WriteLine($"      ✓ Logged to audit trail");
            },
            handlerName: "AuditAlertHandler"
        );
    }
}
