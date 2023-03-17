using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotnetEventBus.Configuration;
using DotnetEventBus.Services;

public class Startup
{
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

public class MyOrderService
{
    private readonly IEventBus _bus;

    public MyOrderService(IEventBus bus)
    {
        _bus = bus;
    }

    public async Task CreateOrderAsync(string orderId)
    {
        // Use the event bus injected via constructor
        await _bus.PublishAsync(new OrderPlacedEvent(orderId, 100.0m));
    }
}
