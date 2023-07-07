# EventMessageExtensions

Provides utility methods for working with `EventMessage` instances, including cloning, header manipulation, and retry attempt tracking.

## API

### `Clone`

Creates a deep copy of the specified `EventMessage` instance, including all headers and properties.

- **Parameters**
  - `source` – The `EventMessage` to clone.
- **Return Value**
  - A new `EventMessage` instance with the same headers and payload as the source.
- **Exceptions**
  - Throws `ArgumentNullException` if `source` is `null`.

---

### `HasExceededMaxAttempts`

Determines whether the `EventMessage` has exceeded the maximum allowed retry attempts.

- **Parameters**
  - `message` – The `EventMessage` to check.
  - `maxAttempts` – The maximum allowed retry attempts.
- **Return Value**
  - `true` if the message's retry count exceeds `maxAttempts`; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `message` is `null`.

---

### `AddHeaders`

Adds one or more headers to the `EventMessage` without overwriting existing values.

- **Parameters**
  - `message` – The `EventMessage` to modify.
  - `headers` – A dictionary of header names and values to add.
- **Exceptions**
  - Throws `ArgumentNullException` if `message` or `headers` is `null`.

---

### `GetHeaderOrDefault`

Retrieves the value of a specified header or returns a default value if the header does not exist.

- **Parameters**
  - `message` – The `EventMessage` to inspect.
  - `headerName` – The name of the header to retrieve.
  - `defaultValue` – The value to return if the header is not found.
- **Return Value**
  - The header value if it exists; otherwise, `defaultValue`.
- **Exceptions**
  - Throws `ArgumentNullException` if `message` or `headerName` is `null`.

## Usage
