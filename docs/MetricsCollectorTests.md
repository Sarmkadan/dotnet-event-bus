# MetricsCollectorTests
The `MetricsCollectorTests` class is designed to test the functionality of the `MetricsCollector` class, which is responsible for collecting and tracking metrics related to event publishing, handling, and failure. This test class ensures that the `MetricsCollector` accurately records and calculates various metrics, such as publish counts, failure counts, average durations, and success rates.

## API
The `MetricsCollectorTests` class contains the following public members:
* `RecordEventPublished_ShouldIncrementPublishCount`: Tests that the `MetricsCollector` increments the publish count when an event is published.
* `RecordEventPublished_ShouldCalculateAverageDuration`: Tests that the `MetricsCollector` calculates the average duration when an event is published.
* `RecordEventPublished_ShouldTrackMinMaxDuration`: Tests that the `MetricsCollector` tracks the minimum and maximum duration when an event is published.
* `RecordEventFailed_ShouldIncrementFailureCount`: Tests that the `MetricsCollector` increments the failure count when an event fails.
* `RecordEventFailed_ShouldCaptureErrorMessage`: Tests that the `MetricsCollector` captures the error message when an event fails.
* `RecordHandlerExecution_ShouldTrackHandlerMetrics`: Tests that the `MetricsCollector` tracks metrics for handler execution.
* `GetAllEventMetrics_ShouldReturnAllTrackedEvents`: Tests that the `MetricsCollector` returns all tracked event metrics.
* `GetAllHandlerMetrics_ShouldReturnAllTrackedHandlers`: Tests that the `MetricsCollector` returns all tracked handler metrics.
* `GetSuccessRate_ShouldCalculatePercentageCorrectly`: Tests that the `MetricsCollector` calculates the success rate percentage correctly.
* `GetSuccessRate_WithNoExecutions_ShouldReturnZero`: Tests that the `MetricsCollector` returns zero when there are no executions.
* `GetAverageDuration_ShouldCalculateAverageForHandler`: Tests that the `MetricsCollector` calculates the average duration for a handler.
* `Reset_ShouldClearAllMetrics`: Tests that the `MetricsCollector` clears all metrics when reset.
* `RecordEventPublished_WithMultipleEvents_ShouldTrackIndependently`: Tests that the `MetricsCollector` tracks multiple events independently.
* `GetLastFailureTime_ShouldUpdateOnFailure`: Tests that the `MetricsCollector` updates the last failure time when an event fails.
* `GetLastPublishedTime_ShouldUpdateOnPublish`: Tests that the `MetricsCollector` updates the last published time when an event is published.

## Usage
Here are two examples of using the `MetricsCollectorTests` class:
```csharp
// Example 1: Testing event publishing metrics
var metricsCollector = new MetricsCollector();
metricsCollector.RecordEventPublished("Event1", 100);
metricsCollector.RecordEventPublished("Event1", 200);
Assert.AreEqual(2, metricsCollector.GetEventPublishCount("Event1"));

// Example 2: Testing handler execution metrics
var metricsCollector = new MetricsCollector();
metricsCollector.RecordHandlerExecution("Handler1", 50);
metricsCollector.RecordHandlerExecution("Handler1", 100);
Assert.AreEqual(75, metricsCollector.GetAverageDurationForHandler("Handler1"));
```

## Notes
The `MetricsCollectorTests` class assumes that the `MetricsCollector` class is thread-safe, as it does not provide any synchronization mechanisms. However, in a multi-threaded environment, it is recommended to use a thread-safe implementation of the `MetricsCollector` class to avoid concurrency issues. Additionally, the `MetricsCollector` class may throw exceptions if the input parameters are invalid or if there are errors during metric calculation. It is recommended to handle these exceptions accordingly in the production code.
