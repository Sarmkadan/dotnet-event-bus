# Subscription

Represents a registered handler for a specific event type within the event bus. Each subscription binds a delegate to an event type string, carries configuration that controls execution behavior (synchronous or asynchronous, timeout, concurrency, dead‑letter routing), and maintains its own lifecycle state. Subscriptions are created by the event bus during registration and can be individually enabled, disabled, or reconfigured at runtime.

## API

### Properties

- **`public string Id`**  
  Unique identifier for the subscription. Assigned by the event bus at creation time and immutable thereafter.

- **`public string EventType`**  
  The event type name this subscription listens to. Used by the bus to route published events to the correct handlers.

- **`public Delegate Handler`**  
  The delegate that will be invoked when an event of `EventType` is published. The delegate signature must match the expectations of the event bus for the given event type.

- **`public string HandlerName`**  
  Human‑readable name of the handler method, typically derived from the delegate’s method information. Useful for diagnostics and logging.

- **`public bool IsActive`**  
  Indicates whether the subscription is currently active. When `false`, events of the matching type are not delivered to this handler. Defaults to `true` upon creation.

- **`public int Priority`**  
  Execution order relative to other subscriptions for the same event type. Lower numeric values indicate higher priority (executed earlier). The event bus sorts handlers by this value before invocation.

- **`public bool IsAsync`**  
  When `true`, the handler is invoked asynchronously and the bus does not block the publishing thread waiting for completion. When `false`, the handler runs synchronously on the publishing thread (or within the same execution flow).

- **`public TimeSpan? Timeout`**  
  Optional maximum duration allowed for handler execution. If the handler does not complete within this time, the bus may cancel or abandon the invocation depending on its internal policy. A `null` value means no timeout is enforced.

- **`public bool AllowConcurrent`**  
  Controls whether multiple invocations of this handler can run simultaneously. When `false`, the bus must serialize execution so that only one invocation is in progress at a time for this subscription.

- **`public bool SendToDeadLetterOnFailure`**  
  When `true`, an event that causes an unhandled exception in this handler is forwarded to the dead‑letter queue (if configured) rather than being silently dropped or re‑thrown.

- **`public DateTime CreatedAtUtc`**  
  UTC timestamp marking when the subscription was originally created.

### Constructors

- **`public Subscription(...)`**  
  Initializes a new subscription instance. The exact parameter list is determined by the event bus internals; consumers do not call this constructor directly. Subscriptions are obtained through bus registration methods.

### Methods

- **`public void Disable()`**  
  Sets `IsActive` to `false`. Subsequent published events of the matching type will skip this handler until `Enable()` is called. Does not affect in‑flight invocations that have already started.

- **`public void Enable()`**  
  Sets `IsActive` to `true`, allowing the handler to receive events again. No effect if the subscription is already active.

- **`public void SetTimeout(TimeSpan? timeout)`**  
  Updates the `Timeout` value for this subscription. Pass `null` to remove any previously configured timeout. The change takes effect for future invocations; it does not alter the timeout of an invocation already in progress.

## Usage

### Example 1: Disabling and re‑enabling a subscription at runtime

```csharp
// Assume 'bus' is an IEventBus instance and 'subscription' was obtained during registration.
Subscription subscription = bus.Subscribe<OrderPlaced>("order.placed", HandleOrderPlaced);

// Temporarily stop handling events during maintenance.
subscription.Disable();

// ... maintenance work ...

// Resume event processing.
subscription.Enable();
```

### Example 2: Configuring timeout and dead‑letter behavior after registration

```csharp
Subscription subscription = bus.Subscribe<PaymentProcessed>(
    "payment.processed",
    HandlePayment,
    options => options.WithPriority(10).WithAsyncExecution()
);

// Apply a 5‑second timeout and enable dead‑letter routing for unhandled failures.
subscription.SetTimeout(TimeSpan.FromSeconds(5));
// SendToDeadLetterOnFailure is typically set via initial options, but can be assigned directly:
// subscription.SendToDeadLetterOnFailure = true;

// Later, remove the timeout if the handler becomes long‑running.
subscription.SetTimeout(null);
```

## Notes

- **Thread safety:** Members such as `IsActive`, `Timeout`, and `SendToDeadLetterOnFailure` may be read and modified from multiple threads. The event bus implementation is expected to synchronise access where necessary, but external code that reads properties while concurrently calling `Disable()`, `Enable()`, or `SetTimeout()` should be prepared for eventual consistency.
- **Disable/Enable during invocation:** Calling `Disable()` while a handler is already executing does not interrupt that execution. The change only affects subsequent event deliveries.
- **Timeout enforcement:** The `Timeout` value is a contract with the bus, not a guarantee that the underlying delegate will be forcefully aborted. Actual cancellation behaviour depends on the bus implementation and whether the handler cooperates with cancellation tokens.
- **Concurrency and `AllowConcurrent`:** When `AllowConcurrent` is `false`, the bus must serialise invocations. This can introduce back‑pressure if events arrive faster than the handler completes. Setting `AllowConcurrent` to `true` removes that serialisation but requires the handler to be thread‑safe.
- **Dead‑letter interaction:** `SendToDeadLetterOnFailure` has no effect if the bus is not configured with a dead‑letter queue. In that case failures are handled according to the bus’s default error policy.
- **Priority ordering:** Priority is evaluated only among subscriptions for the exact same `EventType`. Subscriptions for different event types are independent.
- **Lifecycle:** Subscriptions are owned by the event bus. Disposing or removing a subscription from the bus typically invalidates further use of the `Subscription` object; consult the specific bus implementation for its unsubscription semantics.
