using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetEventBus.Configuration;
using DotnetEventBus.Services;
using Microsoft.Extensions.DependencyInjection;

public record OrderPlacedEvent(string OrderId, decimal Amount);

public class AdvancedUsageExample
{
    public void ConfigureAndRun()
    {
        // 1. Setup DI container and configure EventBus options
        var services = new ServiceCollection();
        
        services.AddEventBus(options =>
        {
            options.EnableLogging = true;
            options.DefaultTimeout = TimeSpan.FromSeconds(30);
            // Additional configuration options can be set here
        });

        var serviceProvider = services.BuildServiceProvider();
        var bus = serviceProvider.GetRequiredService<IEventBus>();

        // 2. Subscribe with error handling/priority
        bus.Subscribe<OrderPlacedEvent>(async (e, ct) =>
        {
            try
            {
                Console.WriteLine($"Processing order: {e.OrderId}");
                // Business logic
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing order {e.OrderId}: {ex.Message}");
                // The event bus handles re-queuing/DLQ if configured
                throw; 
            }
        }, handlerName: "OrderProcessor", priority: 10);
    }
}
