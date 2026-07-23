#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

// Example demonstrating the IEventTransport abstraction usage

using DotnetEventBus.Configuration;
using DotnetEventBus.Models;
using DotnetEventBus.Transport;
using Microsoft.Extensions.DependencyInjection;

// Example 1: Using in-process transport (default)
public class InProcessTransportExample
{
    public static void Run()
    {
        var services = new ServiceCollection();

        // Configure with in-process transport
        services.AddInProcessTransport();
        services.AddEventBus(); // This is called by AddInProcessTransport

        var serviceProvider = services.BuildServiceProvider();

        // Get the transport registry
        var transportRegistry = serviceProvider.GetRequiredService<ITransportRegistry>();

        // Get the default transport
        var defaultTransport = transportRegistry.DefaultTransport;
        Console.WriteLine($"Default transport: {defaultTransport.TransportType}");

        // Publish an event using the transport
        var envelope = EventEnvelope.Create("order.created", new { OrderId = "123", Amount = 99.99 });
        var result = defaultTransport.PublishAsync(envelope).Result;

        Console.WriteLine($"Publish result: {result.Success}, EventId: {result.EventId}");
    }
}

// Example 2: Using webhook transport
public class WebhookTransportExample
{
    public static void Run()
    {
        var services = new ServiceCollection();

        // Configure with webhook transport
        services.AddWebhookTransport("my-secret-key");
        services.AddEventBus(); // This is called by AddWebhookTransport

        var serviceProvider = services.BuildServiceProvider();

        // Get the webhook transport
        var webhookTransport = serviceProvider.GetRequiredService<IEventTransport>();

        // Register a webhook subscription
        var webhookSubscription = new WebhookSubscription
        {
            Url = "https://api.example.com/webhooks/events",
            EventTypes = { "order.created", "order.updated", "payment.processed" },
            IsActive = true,
            MaxRetryAttempts = 3,
            InitialRetryDelay = TimeSpan.FromSeconds(1)
        };

        if (webhookTransport is WebhookTransport webhookTransportImpl)
        {
            webhookTransportImpl.Subscribe(webhookSubscription);
        }

        // Publish an event using the webhook transport
        var envelope = EventEnvelope.Create("order.created", new { OrderId = "456", Amount = 149.99 });
        var result = webhookTransport.PublishAsync(envelope).Result;

        Console.WriteLine($"Webhook publish result: {result.Success}, EventId: {result.EventId}");
    }
}

// Example 3: Using multiple transports with transport registry
public class MultiTransportExample
{
    public static void Run()
    {
        var services = new ServiceCollection();

        // Configure multiple transports
        services.AddInProcessTransport();
        services.AddWebhookTransport("my-secret-key");

        // Configure which transport should be the default
        services.ConfigureTransportRegistry("in-process-transport");

        var serviceProvider = services.BuildServiceProvider();

        // Get the transport registry
        var transportRegistry = serviceProvider.GetRequiredService<ITransportRegistry>();

        // Get all transports
        var allTransports = transportRegistry.GetAllTransports();
        Console.WriteLine($"Registered transports: {allTransports.Count()}");

        foreach (var transport in allTransports)
        {
            var status = transport.GetStatus();
            Console.WriteLine($"Transport {transport.TransportId}: {status.IsHealthy}, " +
                           $"Messages: {status.MessagesPublished}, " +
                           $"Failed: {status.FailedPublishes}");
        }

        // Use the default transport
        var defaultTransport = transportRegistry.DefaultTransport;
        Console.WriteLine($"Using default transport: {defaultTransport.TransportType}");
    }
}

// Example 4: Circuit breaker/retry policy wrapping transports
public class TransportWithPoliciesExample
{
    public static async Task RunAsync()
    {
        var services = new ServiceCollection();

        // Configure transports
        services.AddInProcessTransport();
        services.AddWebhookTransport("my-secret-key");
        services.ConfigureTransportRegistry("in-process-transport");

        var serviceProvider = services.BuildServiceProvider();
        var transportRegistry = serviceProvider.GetRequiredService<ITransportRegistry>();

        // Simulate circuit breaker pattern
        var transport = transportRegistry.DefaultTransport;

        try
        {
            // Publish with retry logic
            for (int i = 0; i < 3; i++)
            {
                var envelope = EventEnvelope.Create("test.event", new { Data = "test" });
                var result = await transport.PublishAsync(envelope);

                if (!result.Success)
                {
                    Console.WriteLine($"Publish attempt {i + 1} failed: {result.ErrorMessage}");
                    await Task.Delay(1000); // Wait before retry
                }
                else
                {
                    Console.WriteLine($"Publish successful: {result.EventId}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"All retry attempts exhausted: {ex.Message}");
        }
    }
}