# PublishResultTests
The `PublishResultTests` class is designed to test the functionality of the `PublishResult` type, which represents the outcome of publishing an event to an event bus. This class contains a set of test methods that verify the correctness of the `PublishResult` type in various scenarios, including successful and failed event handling, creation of failed results, and generation of result summaries.

## API
The `PublishResultTests` class provides the following public members:
* `AddSuccessfulHandler_ShouldIncrementHandlersInvokedAndAppendToList`: Verifies that adding a successful handler increments the count of invoked handlers and appends the handler to the list of successful handlers.
* `AddFailedHandler_ShouldIncrementFailedCountAndCaptureFirstException`: Verifies that adding a failed handler increments the count of failed handlers and captures the first exception that occurred.
* `CreateFailed_ShouldPopulateExceptionAndMarkAsUnsuccessful`: Verifies that creating a failed result populates the exception and marks the result as unsuccessful.
* `GetSummary_ShouldIncludeMessageIdAndSuccessIndicator`: Verifies that getting a summary of the result includes the message ID and a success indicator.

## Usage
Here are two examples of using the `PublishResultTests` class:
```csharp
// Example 1: Testing successful event handling
var result = new PublishResult();
result.AddSuccessfulHandler(() => { });
result.AddSuccessfulHandler(() => { });
Assert.AreEqual(2, result.HandlersInvoked);

// Example 2: Testing failed event handling
var result = new PublishResult();
result.AddFailedHandler(() => { throw new Exception("Test exception"); });
Assert.AreEqual(1, result.FailedCount);
Assert.IsNotNull(result.FirstException);
```

## Notes
When using the `PublishResultTests` class, note that the test methods are designed to be executed independently and do not have any dependencies on each other. However, the `AddSuccessfulHandler` and `AddFailedHandler` methods modify the state of the `PublishResult` instance, so care should be taken to ensure that the instance is properly reset or recreated between test executions. Additionally, the `CreateFailed` method sets the exception and unsuccessful flag on the `PublishResult` instance, which may have implications for thread safety if the instance is shared between multiple threads.
