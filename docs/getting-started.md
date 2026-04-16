# Getting Started with DotnetEventBus

This guide will help you get up and running with DotnetEventBus in just a few minutes.

## Prerequisites

- .NET 10.0 SDK or later
- C# 13.0 or later
- Basic understanding of pub-sub messaging patterns

## Installation

### Via NuGet Package Manager

```bash
dotnet add package DotnetEventBus
```

### Via .csproj

Add the following to your `.csproj` file:

```xml
<ItemGroup>
    <PackageReference Include="DotnetEventBus" Version="1.2.0" />
</ItemGroup>
```

Then run:

```bash
dotnet restore
```

### From Source

Clone the repository and build locally:

```bash
git clone https://github.com/Sarmkadan/dotnet-event-bus.git
cd dotnet-event-bus
dotnet build -c Release
```

## 5-Minute Quick Start

### 1. Define Your Events

```csharp
namespace MyApp.Events;

public class UserCreatedEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WelcomeEmailSentEvent
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public DateTime SentAt { get; set; }
}
```

### 2. Configure the Event Bus

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotnetEventBus;

// Create service collection
var services = new ServiceCollection();

// Add event bus
services.AddEventBus(options =>
{
    options.MaxRetryAttempts = 3;
    options.AllowParallelHandling = true;
    options.EnableDeadLetterQueue = true;
});

// Build provider
var serviceProvider = services.BuildServiceProvider();
var eventBus = serviceProvider.GetRequiredService<IEventBus>();
```

### 3. Create Event Handlers

```csharp
using DotnetEventBus.Handlers;
using Microsoft.Extensions.Logging;

namespace MyApp.Handlers;

// Handler option 1: Class-based
public class SendWelcomeEmailHandler : EventHandlerBase<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(
        IEmailService emailService,
        ILogger<SendWelcomeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public override async Task Handle(
        UserCreatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Sending welcome email to {0}", @event.Email);
        
        await _emailService.SendWelcomeEmailAsync(
            @event.Email,
            @event.FullName,
            cancellationToken);
    }
}

// Handler option 2: Delegate
eventBus.Subscribe<UserCreatedEvent>(
    async (@event, ct) =>
    {
        Console.WriteLine($"User registered: {0}", @event.FullName);
        await Task.CompletedTask;
    },
    handlerName: "LogUserRegistration"
);
```

### 4. Publish Events

```csharp
var newUser = new UserCreatedEvent
{
    UserId = "user-123",
    Email = "john@example.com",
    FullName = "John Doe",
    CreatedAt = DateTime.UtcNow
};

var result = await eventBus.PublishAsync(newUser);

Console.WriteLine($"Event published to {0} handlers", result.HandlersInvoked);
```

## Common Patterns

### Pattern 1: Registration Workflow

```csharp
// Event definitions
public class RegistrationInitiatedEvent
{
    public string Email { get; set; }
    public string Username { get; set; }
}

public class VerificationEmailSentEvent
{
    public string Email { get; set; }
    public string VerificationCode { get; set; }
}

public class RegistrationCompletedEvent
{
    public string Email { get; set; }
    public string UserId { get; set; }
}

// Handlers
public class SendVerificationEmailHandler : EventHandlerBase<RegistrationInitiatedEvent>
{
    private readonly IEmailService _email;
    private readonly IEventBus _eventBus;

    public override async Task Handle(RegistrationInitiatedEvent @event, CancellationToken ct)
    {
        var code = GenerateVerificationCode();
        
        await _email.SendAsync(
            to: @event.Email,
            subject: "Verify Your Email",
            body: $"Code: {code}",
            cancellationToken: ct);

        await _eventBus.PublishAsync(
            new VerificationEmailSentEvent
            {
                Email = @event.Email,
                VerificationCode = code
            },
            ct);
    }
}

// Usage
var registration = new RegistrationInitiatedEvent
{
    Email = "user@example.com",
    Username = "johndoe"
};

await eventBus.PublishAsync(registration);
```

### Pattern 2: Error Handling with Dead Letter Queue

```csharp
public class ProcessPaymentHandler : EventHandlerBase<PaymentInitiatedEvent>
{
    private readonly IPaymentGateway _gateway;

    public override async Task Handle(PaymentInitiatedEvent @event, CancellationToken ct)
    {
        try
        {
            var result = await _gateway.ProcessAsync(@event.Amount, ct);
            
            if (!result.IsSuccessful)
                throw new InvalidOperationException($"Payment failed: {result.Error}");
        }
        catch (TimeoutException ex)
        {
            // Will be retried automatically
            throw;
        }
        catch (Exception ex)
        {
            // Will go to dead letter queue
            throw;
        }
    }
}

// Check dead letter queue
var dlq = serviceProvider.GetRequiredService<IDeadLetterService>();
var failed = await dlq.GetPendingEntriesAsync();

foreach (var entry in failed)
{
    Console.WriteLine($"Failed: {entry.EventType}");
    Console.WriteLine($"Attempts: {entry.RetryCount}");
    Console.WriteLine($"Last Error: {entry.LastException}");
}

// Retry a specific entry
await dlq.ReprocessEntryAsync(failed.First().Id);
```

### Pattern 3: Multiple Handlers with Priority

```csharp
// High priority - critical operations
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) => await ValidateInventory(@event, ct),
    handlerName: "InventoryValidator",
    priority: 100
);

// Normal priority
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) => await SendConfirmationEmail(@event, ct),
    handlerName: "EmailNotification",
    priority: 50
);

// Low priority - optional operations
eventBus.Subscribe<OrderCreatedEvent>(
    async (@event, ct) => await UpdateAnalytics(@event, ct),
    handlerName: "AnalyticsTracker",
    priority: 10
);

// Handlers execute in order: InventoryValidator → EmailNotification → AnalyticsTracker
```

## Testing

### Unit Testing Handlers

```csharp
using Xunit;
using Moq;

public class SendWelcomeEmailHandlerTests
{
    [Fact]
    public async Task Handle_SendsEmailWhenUserCreated()
    {
        // Arrange
        var mockEmailService = new Mock<IEmailService>();
        var mockLogger = new Mock<ILogger<SendWelcomeEmailHandler>>();
        
        var handler = new SendWelcomeEmailHandler(
            mockEmailService.Object,
            mockLogger.Object);

        var @event = new UserCreatedEvent
        {
            UserId = "user-1",
            Email = "test@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        mockEmailService.Verify(
            x => x.SendWelcomeEmailAsync("test@example.com", "Test User", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

### Integration Testing Event Bus

```csharp
public class EventBusIntegrationTests
{
    [Fact]
    public async Task PublishAsync_InvokesAllSubscribedHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEventBus();
        var provider = services.BuildServiceProvider();
        var eventBus = provider.GetRequiredService<IEventBus>();

        var handlerInvoked = false;
        
        eventBus.Subscribe<TestEvent>(
            async (@event, ct) =>
            {
                handlerInvoked = true;
                await Task.CompletedTask;
            },
            handlerName: "TestHandler");

        // Act
        var result = await eventBus.PublishAsync(new TestEvent());

        // Assert
        Assert.True(handlerInvoked);
        Assert.Equal(1, result.HandlersInvoked);
    }
}
```

## Next Steps

1. **Explore Examples**: Check the `/examples` directory for complete sample applications
2. **Read Architecture Guide**: See `docs/architecture.md` for detailed system design
3. **API Reference**: Review `docs/api-reference.md` for complete API documentation
4. **Deployment Guide**: Check `docs/deployment.md` for production deployment strategies
5. **FAQ**: See `docs/faq.md` for common questions and troubleshooting

## Getting Help

- **Issues**: Open an issue on [GitHub](https://github.com/Sarmkadan/dotnet-event-bus/issues)
- **Discussions**: Join [GitHub Discussions](https://github.com/Sarmkadan/dotnet-event-bus/discussions)
- **Contact**: Reach out to [@Sarmkadan](https://t.me/sarmkadan) on Telegram

## Resources

- [GitHub Repository](https://github.com/Sarmkadan/dotnet-event-bus)
- [NuGet Package](https://www.nuget.org/packages/DotnetEventBus)
- [Author Portfolio](https://sarmkadan.com)
