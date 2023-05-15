# DeadLetterBenchmarks

The `DeadLetterBenchmarks` class provides benchmarking utilities for evaluating the performance and behavior of dead-letter queue (DLQ) operations in the `dotnet-event-bus` project. It measures key scenarios such as publishing to the dead-letter queue, retrieving pending entries, reprocessing entries, and memory allocation during these operations. This class is primarily used for performance testing and validation of the event bus's dead-letter handling mechanisms.

## API

### `public string EventId`
Gets or sets the unique identifier of the event being processed. This property is used to track individual events during benchmarking.

### `public string Name`
Gets or sets the name of the event or handler associated with the benchmark. This is typically used for logging or identification purposes.

### `public int AttemptCount`
Gets or sets the number of attempts made to process the event before it was moved to the dead-letter queue. This property helps analyze retry behavior.

### `public FailingHandler`
Represents the handler that intentionally fails during benchmarking to simulate dead-letter scenarios. This property is used to configure the handler that will trigger dead-letter operations.

### `public Task Handle`
The task representing the execution of the failing handler. This is used to simulate and measure the handling process that leads to dead-lettering.

### `public string GetHandlerName()`
Returns the name of the failing handler. This is useful for logging or debugging purposes during benchmark execution.

### `public void GlobalSetup()`
Performs global setup operations required before running benchmarks. This may include initializing services, configuring the event bus, or setting up test data. This method should be called once before executing any benchmarks.

### `public void GlobalCleanup()`
Performs cleanup operations after benchmark execution. This may include disposing of resources, resetting state, or tearing down test configurations. This method should be called once after all benchmarks have completed.

### `public async Task Publish_To_DeadLetter()`
Benchmarks the process of publishing an event directly to the dead-letter queue. This measures the performance and overhead of dead-lettering an event without retry attempts.

### `public async Task Get_Pending_DeadLetter_Entries()`
Benchmarks the retrieval of pending dead-letter entries. This measures the performance of querying the dead-letter queue for events that require reprocessing or analysis.

### `public async Task Reprocess_10_DeadLetter_Entries()`
Benchmarks the reprocessing of 10 dead-letter entries. This measures the performance of retrieving and re-attempting to process dead-lettered events.

### `public async Task Get_DeadLetter_Statistics()`
Benchmarks the retrieval of statistics related to the dead-letter queue, such as the total number of entries, failed attempts, or other metrics. This helps evaluate the performance of administrative operations on the DLQ.

### `public async Task Publish_With_Retry_Policy()`
Benchmarks the process of publishing an event with a retry policy, where the event is eventually moved to the dead-letter queue after exhausting retry attempts. This measures the performance of retry mechanisms and their impact on dead-lettering.

### `public async Task DeadLetter_Memory_Allocation()`
Benchmarks the memory allocation patterns during dead-letter operations. This measures the memory overhead of dead-lettering events, including any temporary allocations during processing.

## Usage

### Example 1: Benchmarking Dead-Letter Publishing and Reprocessing
