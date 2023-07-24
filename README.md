// ... (rest of the README.md content remains the same)

## EventBusBuilder

The `EventBusBuilder` class is a fluent builder for configuring and creating the event bus. It allows you to customize various settings, such as event message repositories, subscription repositories, dead letter repositories, and distributed event bus settings.

Example usage:
```csharp
var services = new ServiceCollection();
var eventBusBuilder = services.AddEventBusBuilder()
    .WithOptions(options => options.DefaultHandlerTimeout = TimeSpan.FromSeconds(30))
    .WithMessageRepository(new InMemoryEventMessageRepository())
    .WithSubscriptionRepository(new InMemorySubscriptionRepository())
    .WithDeadLetterRepository(new InMemoryDeadLetterRepository())
    .WithMaxRetries(5)
    .WithParallelHandling(true)
    .WithMaxConcurrentHandlers(10)
    .WithDeadLetterQueue(true)
    .WithThrowOnHandlerFailure(true)
    .AsDistributed("rabbitmq", "amqp://guest:guest@localhost:5672")
    .Build();

var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetService<IEventBus>();
```

```