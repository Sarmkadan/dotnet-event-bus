# WebhookHandler

A utility class for managing webhook subscriptions, generating and verifying signatures, and handling event delivery with retry logic. It provides methods to subscribe, unsubscribe, and query webhook subscriptions, as well as utilities for secure signature validation.

## API

### `WebhookHandler()`
Initializes a new instance of the `WebhookHandler` class with default retry settings.

### `Subscribe(WebhookSubscription subscription)`
Subscribes a webhook to one or more event types.

- **Parameters**
  - `subscription`: The `WebhookSubscription` instance containing the webhook URL, event types, headers, and other metadata.
- **Returns**
  - `void`
- **Throws**
  - `ArgumentNullException`: If `subscription` is `null`.
  - `ArgumentException`: If `subscription.Url` is invalid or `subscription.EventTypes` is empty.

### `Unsubscribe(string id)`
Removes a webhook subscription by its unique identifier.

- **Parameters**
  - `id`: The unique identifier of the subscription to remove.
- **Returns**
  - `bool`: `true` if the subscription was found and removed; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `id` is `null`.

### `GetWebhooksForEvent(string eventType)`
Retrieves all active subscriptions that are registered for a specific event type.

- **Parameters**
  - `eventType`: The event type to filter subscriptions by.
- **Returns**
  - `IEnumerable<WebhookSubscription>`: A collection of matching subscriptions.
- **Throws**
  - `ArgumentNullException`: If `eventType` is `null`.

### `GenerateSignature(string payload, string secret)`
Generates a HMAC SHA-256 signature for a given payload using a secret key.

- **Parameters**
  - `payload`: The payload string to sign.
  - `secret`: The secret key used for signing.
- **Returns**
  - `string`: The generated signature in hexadecimal format.
- **Throws**
  - `ArgumentNullException`: If `payload` or `secret` is `null`.

### `VerifySignature(string payload, string secret, string signature)`
Verifies that a payload matches the expected signature.

- **Parameters**
  - `payload`: The payload string to verify.
  - `secret`: The secret key used for verification.
  - `signature`: The expected signature to compare against.
- **Returns**
  - `bool`: `true` if the signature is valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `payload`, `secret`, or `signature` is `null`.

### `GetAllSubscriptions()`
Retrieves all active and inactive webhook subscriptions.

- **Returns**
  - `IEnumerable<WebhookSubscription>`: A collection of all subscriptions.
- **Throws**
  - None.

### `UpdateSubscription(WebhookSubscription subscription)`
Updates an existing webhook subscription.

- **Parameters**
  - `subscription`: The updated `WebhookSubscription` instance.
- **Returns**
  - `bool`: `true` if the subscription was found and updated; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `subscription` is `null`.
  - `ArgumentException`: If `subscription.Id` is `null` or empty.

### `Id` (property)
Gets the unique identifier of the subscription.

- **Type**
  - `string`
- **Access**
  - Read-only.

### `Url` (property)
Gets or sets the webhook endpoint URL.

- **Type**
  - `string`
- **Access**
  - Read-write.
- **Throws**
  - `ArgumentNullException`: If `value` is `null` when setting.

### `EventTypes` (property)
Gets or sets the list of event types this webhook is subscribed to.

- **Type**
  - `List<string>`
- **Access**
  - Read-write.

### `Headers` (property)
Gets or sets the custom HTTP headers to include with each webhook request.

- **Type**
  - `Dictionary<string, string>`
- **Access**
  - Read-write.

### `IsActive` (property)
Gets or sets whether the subscription is currently active.

- **Type**
  - `bool`
- **Access**
  - Read-write.

### `RetryCount` (property)
Gets or sets the maximum number of retry attempts for failed deliveries.

- **Type**
  - `int`
- **Access**
  - Read-write.

### `RetryDelay` (property)
Gets or sets the delay between retry attempts.

- **Type**
  - `TimeSpan`
- **Access**
  - Read-write.

### `CreatedAt` (property)
Gets the timestamp when the subscription was created.

- **Type**
  - `DateTime`
- **Access**
  - Read-only.

### `LastDeliveryAt` (property)
Gets the timestamp of the last delivery attempt.

- **Type**
  - `DateTime?`
- **Access**
  - Read-only.

### `LastDeliveryStatus` (property)
Gets the status of the last delivery attempt.

- **Type**
  - `string?`
- **Access**
  - Read-only.

## Usage

### Example 1: Subscribing to an Event
