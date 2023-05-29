# RequestResponseBus

A message bus implementation that supports request/response communication patterns in .NET applications. It enables sending requests with payloads and receiving responses asynchronously, while managing request lifecycle including timeouts, cancellation, and metadata propagation.

## API

### `RequestResponseBus`
The base class for request/response bus implementations. Provides core functionality for sending requests, handling responses, and managing request state.

### `public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest payload, TimeSpan? timeout = null, Dictionary<string, object>? metadata = null)`
Sends a request with the specified payload and optional timeout/metadata, returning the response asynchronously.

- **Parameters**:
  - `payload` (`TRequest`): The request payload to send.
  - `timeout` (`TimeSpan?`, optional): The timeout for the request. Defaults to the bus's default timeout if not specified.
  - `metadata` (`Dictionary<string, object>?`, optional): Additional metadata to associate with the request.
- **Return value**: A `Task<TResponse?>` representing the asynchronous operation. The task completes when a response is received or the request times out/cancels.
- **Exceptions**: Throws `ArgumentNullException` if `payload` is `null`.

### `public bool SendResponse(RequestContext context, TResponse response)`
Sends a response for a pending request.

- **Parameters**:
  - `context` (`RequestContext`): The request context to respond to.
  - `response` (`TResponse`): The response payload to send.
- **Return value**: `true` if the response was sent successfully; `false` if the request was already completed or invalid.
- **Exceptions**: Throws `ArgumentNullException` if `context` or `response` is `null`.

### `public bool FailRequest(RequestContext context, string errorMessage)`
Fails a pending request with an error message.

- **Parameters**:
  - `context` (`RequestContext`): The request context to fail.
  - `errorMessage` (`string`): The error message describing the failure.
- **Return value**: `true` if the request was failed successfully; `false` if the request was already completed or invalid.
- **Exceptions**: Throws `ArgumentNullException` if `context` or `errorMessage` is `null`.

### `public int GetPendingRequestCount()`
Gets the number of currently pending (uncompleted) requests.

- **Return value**: The count of pending requests.

### `public void CancelAllRequests()`
Cancels all pending requests, completing their tasks with a `TaskCanceledException`.

### `public abstract Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, RequestContext context)`
Handles an incoming request asynchronously.

- **Parameters**:
  - `request` (`TRequest`): The request payload to handle.
  - `context` (`RequestContext`): The request context containing metadata and state.
- **Return value**: A `Task<TResponse>` representing the asynchronous handling operation.
- **Exceptions**: Throws `ArgumentNullException` if `request` or `context` is `null`.

### `public async Task ProcessRequestAsync<TRequest, TResponse>(RequestContext context)`
Processes a request asynchronously, invoking the handler and managing the response lifecycle.

- **Parameters**:
  - `context` (`RequestContext`): The request context to process.
- **Exceptions**: Throws `ArgumentNullException` if `context` is `null`.

### `public string? RequestId`
Gets the unique identifier for the request. Read-only.

### `public required T Payload`
Gets the request payload. Required and read-only.

### `public Dictionary<string, object> Metadata`
Gets the metadata associated with the request. Mutable during handling.

### `public DateTime CreatedAt`
Gets the timestamp when the request was created. Read-only.

### `public TimeSpan Timeout`
Gets or sets the timeout for the request. Defaults to the bus's default timeout if not specified.

### `public bool Success`
Gets or sets whether the request completed successfully. Read-only after completion.

### `public string? ErrorMessage`
Gets the error message if the request failed. `null` if successful or pending. Read-only after completion.

## Usage

### Example 1: Basic Request/Response
