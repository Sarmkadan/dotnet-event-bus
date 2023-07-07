# OrderCreatedEvent

`OrderCreatedEvent` is a domain event that represents the creation of an order within the system. It carries all essential data about the order, such as identifiers, monetary value, and product details, allowing subscribers to react to order creation (e.g., inventory reservation, billing, or notification).

## API

### Fields

| Member | Type | Purpose |
|--------|------|---------|
| `EventId` | `string` | Unique identifier for the event instance. Used for deduplication and tracing. |
| `Timestamp` | `DateTime` | UTC date and time when the event was raised. |
| `OrderId` | `string` | Identifier of the order that was created. |
| `CustomerId` | `string` | Identifier of the customer who placed the order. |
| `Amount` | `decimal` | Total monetary value of the order, in the currency used by the system. |
| `ProductName` | `string` | Name of the product associated with the order. |
| `TransactionId` | `string` | Identifier of the underlying payment or transaction record, if any. |
| `IsSuccessful` | `bool` | Flag indicating whether the order creation operation succeeded. |

> **Note:** The source declares `EventId`, `Timestamp`, and `OrderId` twice each. These duplicate declarations refer to the same logical members; they are documented once above.

### Methods

| Member | Signature | Parameters | Return Value | Throws |
|--------|-----------|------------|--------------|--------|
| `Handle` | `public override async Task Handle()` | None | A `Task` representing the asynchronous operation. | May throw any exception thrown by the handling logic (e.g., `InvalidOperationException` if required data is missing, or `TimeoutException` if an external call fails). |
| `Handle` | `public override async Task Handle()` | None | A `Task` representing the asynchronous operation. | Same as above. |

> **Note:** Two `Handle` overloads with identical signatures are present in the type. Both provide the same asynchronous handling contract; callers can treat them interchangeably.

## Usage

### Publishing an OrderCreatedEvent

```csharp
using System;
using DotNetEventBus.Events;

var @event = new OrderCreatedEvent
{
    EventId      = Guid.NewGuid().ToString(),
    Timestamp    = DateTime.UtcNow,
    OrderId      = "ORD-12345",
    CustomerId   = "CUST-67890",
    Amount       = 149.99m,
    ProductName  = "Wireless Headphones",
    TransactionId = "TXN-98765",
    IsSuccessful = true
};

await eventBus.PublishAsync(@event);
```

### Handling an OrderCreatedEvent

```csharp
using System.Threading.Tasks;
using DotNetEventBus.Events;

public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public override async Task Handle(OrderCreatedEvent @event)
    {
        // Example: reserve inventory for the ordered product.
        if (!@event.IsSuccessful)
        {
            // Skip further processing for failed orders.
            return;
        }

        await inventoryService.ReserveAsync(@event.ProductName, 1);
        await notificationService.SendOrderConfirmationAsync(@event.CustomerId, @event.OrderId);
    }
}
```

## Notes

- The fields are publicly settable; therefore, instances are **not** immutable. Concurrent modification of the same instance from multiple threads without external synchronization can lead to race conditions. It is recommended to treat each event instance as immutable after publication.
- Decimal values should be validated for correct scale and precision before assignment to avoid unexpected rounding in downstream systems.
- The `Handle` methods do not declare any parameters; they rely on the instance data passed via the event object. If required fields are `null` or contain invalid values (e.g., negative `Amount`), the handler may throw exceptions—callers should ensure the event is properly populated before publishing.
- Duplicate field declarations (`EventId`, `Timestamp`, `OrderId`) do not affect runtime behavior but may cause compiler warnings if the same member is defined in multiple base types. The documented members represent the logical contract of the type.
