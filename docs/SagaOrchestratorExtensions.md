# SagaOrchestratorExtensions
The `SagaOrchestratorExtensions` class provides a set of extension methods for working with saga orchestrators in the `dotnet-event-bus` project. These methods enable developers to customize and execute saga orchestrators, as well as extract relevant information from them.

## API
* `public static SagaOrchestrator<TContext> WithName<TContext>(...)`: This method creates a new saga orchestrator with a specified name. The purpose of this method is to allow for the creation of named saga orchestrators, which can be useful for identification and logging purposes. The method takes a type parameter `TContext` and returns a new `SagaOrchestrator<TContext>` instance. The parameters and return value are not explicitly defined in the provided information, so their exact nature is unclear.
* `public static async Task<SagaExecutionResult> ExecuteAsync<TContext>(...)`: This asynchronous method executes a saga orchestrator and returns the result of the execution. The purpose of this method is to provide a way to execute a saga orchestrator and retrieve the outcome of the execution. The method takes a type parameter `TContext` and returns a `Task<SagaExecutionResult>`. The parameters are not explicitly defined, so their exact nature is unclear. This method may throw exceptions if the execution of the saga orchestrator fails.
* `public static Dictionary<string, object> ToDictionary`: This property converts a saga orchestrator to a dictionary. The purpose of this property is to provide a way to serialize or deserialize a saga orchestrator. The property returns a `Dictionary<string, object>`. The exact parameters and potential exceptions are not explicitly defined.
* `public static IEnumerable<SagaStep<TContext>> GetFailedSteps<TContext>`: This method retrieves the failed steps of a saga orchestrator. The purpose of this method is to provide a way to diagnose and handle failures in a saga orchestrator. The method takes a type parameter `TContext` and returns an `IEnumerable<SagaStep<TContext>>`. The parameters are not explicitly defined, so their exact nature is unclear.

## Usage
The following examples demonstrate how to use the `SagaOrchestratorExtensions` class:
```csharp
// Example 1: Creating a named saga orchestrator
var orchestrator = SagaOrchestratorExtensions.WithName<MyContext>("MyOrchestrator");

// Example 2: Executing a saga orchestrator and handling the result
var executionResult = await SagaOrchestratorExtensions.ExecuteAsync<MyContext>(orchestrator);
if (executionResult.Success)
{
    Console.WriteLine("Saga orchestrator executed successfully.");
}
else
{
    Console.WriteLine("Saga orchestrator execution failed.");
}
```

## Notes
When using the `SagaOrchestratorExtensions` class, consider the following edge cases and thread-safety remarks:
* The `ExecuteAsync` method is asynchronous, which means it can be executed concurrently with other tasks. However, the method may not be thread-safe if the underlying saga orchestrator is not designed to handle concurrent execution.
* The `ToDictionary` property may not preserve the original structure or behavior of the saga orchestrator, as it converts the orchestrator to a dictionary. This may have implications for serialization or deserialization.
* The `GetFailedSteps` method may return an empty enumerable if no steps have failed, or if the saga orchestrator does not support failure tracking.
* The `WithName` method may throw an exception if the specified name is invalid or already in use. However, the exact exception type and behavior are not explicitly defined.
