# SagaOrchestrator
The `SagaOrchestrator<TContext>` class implements the Saga pattern for managing distributed transactions across multiple services or steps. It orchestrates a sequence of steps, automatically triggering compensating actions (rollbacks) when any step fails, ensuring eventual consistency. Each step can define a forward action and an optional compensation action to undo its effects.

## API
### Instance Members
* `new SagaOrchestrator<TContext>(sagaId, logger?)`: Creates a new saga orchestrator with the specified saga ID and optional logger.
* `Name`: Gets or sets the display name of the saga (defaults to the saga ID).
* `AddStep(stepName, action, compensationAction?)`: Adds a step to the saga with an optional compensation action. Returns the orchestrator for chaining.
* `ExecuteAsync(context)`: Executes the saga orchestration. If any step fails, it triggers compensation actions for all completed steps in reverse order. Returns a `SagaExecutionResult`.
* `GetStepStatus()`: Returns a snapshot of the current status of every step in the saga.

### Nested Types
* `SagaStep<TContext>`: Represents a single step in the saga.
  * `Name`: Name of the step.
  * `Action`: The async action to execute for this step.
  * `CompensationAction`: Optional async compensation action to undo the step.
  * `Status`: Current execution status (see `SagaStepStatus`).
  * `ErrorMessage`: Error message if the step failed.
* `SagaStepStatus`: Enumeration of possible step statuses.
  * `Pending`: Step added but not started.
  * `Running`: Step currently executing.
  * `Completed`: Step finished successfully.
  * `Compensating`: Step's compensation action is executing.
  * `Compensated`: Step's compensation action completed successfully.
  * `Failed`: Step failed during execution.
  * `CompensationFailed`: Step's compensation action failed.
* `SagaExecutionResult`: Result of saga execution.
  * `SagaId`: Identifier of the saga.
  * `Success`: Whether the saga completed successfully.
  * `FailedStep`: Name of the step that failed (if any).
  * `Error`: Error message describing the failure (if any).
  * `ExecutedAt`: Timestamp (UTC) when execution finished.

### Exceptions
* `SagaCompensationException`: Thrown when saga execution fails and one or more compensation actions also fail during rollback. Contains:
  * `OriginalException`: The exception that caused the saga to fail.
  * `FailedStepName`: Name of the step that failed during forward execution.
  * `CompensationFailures`: Collection of compensation failures that occurred during rollback.
  * `SagaResult`: The saga execution result.
* `CompensationFailure`: Represents a failure during a compensation step.
  * `StepName`: Name of the saga step that failed during compensation.
  * `Exception`: The exception that occurred during compensation.

## Usage
The following examples demonstrate how to utilize the `SagaOrchestrator` class:

### Example 1: Basic Saga with Compensation
```csharp
// Define a context to share data between steps
public class OrderProcessingContext
{
    public string OrderId { get; set; } = null!;
    public bool PaymentProcessed { get; set; }
    public bool InventoryReserved { get; set; }
    public bool NotificationSent { get; set; }
}

// Create the orchestrator
var saga = new SagaOrchestrator<OrderProcessingContext>("order-processing-saga")
{
    Name = "Order Processing Saga"
};

// Add steps with compensation actions
saga.AddStep(
    "ProcessPayment",
    async (ctx) =>
    {
        // Process payment logic
        ctx.PaymentProcessed = true;
        // Simulate potential failure
        // if (somethingWentWrong) throw new Exception("Payment failed");
    },
    async (ctx) =>
    {
        // Compensation: refund payment
        if (ctx.PaymentProcessed)
        {
            // Refund logic here
        }
    }
);

saga.AddStep(
    "ReserveInventory",
    async (ctx) =>
    {
        // Reserve inventory logic
        ctx.InventoryReserved = true;
    },
    async (ctx) =>
    {
        // Compensation: release inventory reservation
        if (ctx.InventoryReserved)
        {
            // Release inventory here
        }
    }
);

saga.AddStep(
    "SendNotification",
    async (ctx) =>
    {
        // Send notification logic
        ctx.NotificationSent = true;
    },
    async (ctx) =>
    {
        // Compensation: send failure notification or cleanup
        // (often no compensation needed for notifications)
    }
);

// Execute the saga
var context = new OrderProcessingContext { OrderId = "ORDER-123" };
var result = await saga.ExecuteAsync(context);

if (result.Success)
{
    Console.WriteLine("Order processed successfully!");
}
else
{
    Console.WriteLine($"Order processing failed at step: {result.FailedStep}");
    Console.WriteLine($"Error: {result.Error}");
}
```

### Example 2: Saga Without Compensation (Idempotent Steps)
```csharp
var saga = new SagaOrchestrator<SimpleContext>("simple-saga");

// Some steps may not need compensation if they are idempotent
// or have no side effects that require rollback
saga.AddStep(
    "ValidateInput",
    async (ctx) =>
    {
        // Validation logic - typically doesn't need compensation
        // if validation fails, we just don't proceed
    }
    // No compensation action needed
);

saga.AddStep(
    "UpdateDatabase",
    async (ctx) =>
    {
        // Database update logic
    },
    async (ctx) =>
    {
        // Compensation: rollback database changes
        // Only needed if the update is not idempotent
    }
);
```

### Example 3: Handling Compensation Failures
```csharp
try
{
    var result = await saga.ExecuteAsync(context);
    
    if (!result.Success)
    {
        // Handle partial failure where some compensations may have failed
        Console.WriteLine($"Saga failed: {result.Error}");
    }
}
catch (SagaCompensationException ex)
{
    // Handle case where both the original step AND its compensation failed
    Console.WriteLine($"Critical failure: {ex.OriginalException.Message}");
    Console.WriteLine($"Failed step: {ex.FailedStepName}");
    
    foreach (var cf in ex.CompensationFailures)
    {
        Console.WriteLine($"Compensation failed for step '{cf.StepName}': {cf.Exception.Message}");
    }
    
    // May require manual intervention
}
```

## Notes
When using the `SagaOrchestrator` class, consider the following:
* The orchestrator is stateful and designed for single-use execution. Create a new instance for each saga execution.
* All steps execute sequentially; if parallel execution is needed, consider modifying the pattern or using additional concurrency constructs within step actions.
* Compensation actions should be designed to be idempotent where possible, as they may be retried in failure scenarios.
* The context (`TContext`) is shared between all steps and should be immutable or carefully managed to avoid unintended side effects.
* If a step fails and its compensation action also fails, a `SagaCompensationException` is thrown, preserving both the original failure and compensation failure details.
* Steps without compensation actions will not trigger rollback logic; failure in such steps will still cause the saga to fail, but no compensation will be attempted for that step.
* The orchestrator uses structured logging via `ILogger` if provided; ensure logging is configured appropriately in your application.
* For long-running sagas, consider persisting saga state to survive application restarts (this implementation does not include persistence).