using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotnetEventBus.Configuration;
using DotnetEventBus.Services;

/// <summary>
/// Provides startup configuration for integrating the event bus with ASP.NET Core or Generic Host applications.
/// </summary>
public class Startup
{
    /// <summary>
/// Configures services for the application, including event bus registration and other dependencies.
/// </summary>
/// <param name="services">The service collection to configure.</param>
public void ConfigureServices(IServiceCollection services)
    {
        // Wire up the event bus into the ASP.NET Core / Generic Host DI container
        services.AddEventBus(options =>
        {
            options.EnableLogging = true;
        });

        // Register other services
        services.AddSingleton<MyOrderService>();
    }
}

/// <summary>
/// Example service that demonstrates how to use the event bus to publish events.
/// </summary>
public class MyOrderService
{
    private readonly IEventBus _bus;

		/// <summary>
		/// Initializes a new instance of the <see cref="MyOrderService"/> class.
		/// </summary>
		/// <param name="bus">The event bus instance for publishing events.</param>

    public MyOrderService(IEventBus bus)
    {
        _bus = bus;
    }

    /// <summary>
		/// Creates an order and publishes an <see cref="OrderPlacedEvent"/> using the event bus.
		/// </summary>
		/// <param name="orderId">The unique identifier for the order.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task CreateOrderAsync(string orderId)
    {
        // Use the event bus injected via constructor
        await _bus.PublishAsync(new OrderPlacedEvent(orderId, 100.0m));
    }
}
