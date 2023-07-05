# DeadLetterQueueHandlingExample

Demonstrates handling of messages that have been routed to a dead-letter queue after exceeding retry limits or encountering irrecoverable processing failures. This type provides two overloads of `Handle` to process dead-lettered events with different contextual information, along with a `Main` entry point that simulates a dead-letter processing pipeline. It is intended for scenarios where undeliverable or repeatedly failed messages must be inspected, logged, or conditionally reprocessed.

## API

### public string OrderId

Gets or sets the identifier of the order associated with the dead-lettered message. Used to correlate the failed event with the originating business entity.

### public decimal Amount

Gets or sets the monetary amount tied to the dead-lettered event. Typically represents the value of the transaction that could not be processed.

### public string PaymentMethod

Gets or sets the payment method string (e.g., "CreditCard", "BankTransfer") from the original event. Useful for determining whether the failure is payment-method-specific.

### public string RecipientId

Gets or sets the identifier of the intended recipient or target system for the original message. Helps trace routing failures.

### public string Message

Gets or sets the raw message payload or error description that landed in the dead-letter queue. May contain serialized event data or a failure summary.

### public string Channel

Gets or sets the logical channel or queue name from which the dead-lettered message was sourced (e.g., "orders", "payments"). Enables channel-specific handling strategies.

### public override async Task Handle

First overload of the dead-letter handler. Processes a dead-lettered event using internal state already populated on the instance (OrderId, Amount, etc.). Throws `InvalidOperationException` when required properties have not been set prior to invocation. Returns a `Task` representing the asynchronous handling operation.

### public override async Task Handle

Second overload accepting a raw dead-letter envelope or context object (exact parameter type determined by the base class contract). Decodes the incoming dead-letter metadata, populates the instance properties, and delegates to the parameterless overload. Throws `ArgumentNullException` when the supplied argument is null, and `FormatException` when the envelope cannot be deserialized. Returns a `Task` representing the asynchronous handling operation.

### public static async Task Main

Entry point that constructs an instance, configures sample dead-letter data, and invokes both `Handle` overloads to demonstrate the processing flow. Accepts the standard `string[] args` parameter. Returns a `Task` suitable for async entry-point execution. Does not throw under normal demonstration conditions; may propagate exceptions from the underlying infrastructure if the simulated bus connection fails.

## Usage

### Example 1: Handling a dead-lettered payment event with the parameterless overload

```csharp
var handler = new DeadLetterQueueHandlingExample
{
    OrderId = "ORD-98765",
    Amount = 249.99m,
    PaymentMethod = "CreditCard",
    RecipientId = "payment-processor-3",
    Message = "{\"error\":\"timeout\"}",
    Channel = "payments"
};

try
{
    await handler.Handle();
    Console.WriteLine("Dead-lettered payment event processed successfully.");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Handler configuration error: {ex.Message}");
}
```

### Example 2: Processing a dead-letter envelope via the parameterized overload

```csharp
var handler = new DeadLetterQueueHandlingExample();
var envelope = new DeadLetterEnvelope
{
    Body = "{\"orderId\":\"ORD-12345\",\"amount\":150.00,\"paymentMethod\":\"BankTransfer\"}",
    DeadLetterReason = "MaxRetryExceeded",
    SourceChannel = "orders"
};

try
{
    await handler.Handle(envelope);
    Console.WriteLine($"Order {handler.OrderId} dead-letter processed. Reason: {envelope.DeadLetterReason}");
}
catch (ArgumentNullException)
{
    Console.WriteLine("Envelope must not be null.");
}
catch (FormatException)
{
    Console.WriteLine("Envelope body is malformed.");
}
```

## Notes

- Both `Handle` overloads are designed for single-threaded consumption per instance; concurrent invocations on the same instance may lead to property state corruption as the parameterized overload writes to the public properties before delegating.
- The parameterless `Handle` throws `InvalidOperationException` when required fields (`OrderId`, `Channel`) are null or empty. Callers should validate configuration before invoking it.
- The parameterized `Handle` performs deserialization internally; malformed payloads result in `FormatException`, while a null argument produces `ArgumentNullException`. Callers should wrap invocations in try-catch blocks when envelope integrity cannot be guaranteed.
- `Main` is intended for demonstration and testing. It does not implement retry or circuit-breaker patterns; production dead-letter handlers should incorporate idempotency checks and side-effect safety.
- No static shared state is mutated by these members, but instance state is mutable. If an instance is reused across multiple dead-letter messages, property values from a prior call may linger and affect subsequent parameterless `Handle` invocations unless explicitly reset.
