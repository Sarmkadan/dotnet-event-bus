# ValidationHelper

Fluent validation helper that accumulates validation errors and allows throwing a single `ValidationException` when any rule fails. It is intended for lightweight, imperative validation scenarios where a collection of error messages is needed before deciding how to proceed.

## API

### `public ValidationHelper RequireNotEmpty(string value, string propertyName = null)`
Adds a rule that fails if `value` is `null` or consists only of white‑space characters. Returns the same `ValidationHelper` instance to enable method chaining. No exception is thrown immediately; the error is recorded and can be retrieved via `GetErrors` or cause `ThrowIfInvalid` to throw.

### `public ValidationHelper RequireNotNull<T>(T value, string propertyName = null) where T : class`
Adds a rule that fails if `value` is `null`. Returns the same `ValidationHelper` instance for chaining. Errors are deferred until validation is executed.

### `public ValidationHelper RequirePattern(string input, string pattern, string propertyName = null)`
Adds a rule that fails if `input` does not match the regular expression supplied in `pattern`. Returns the same `ValidationHelper` instance for chaining. Errors are collected for later reporting.

### `public ValidationHelper RequireLength(string value, int minLength, int maxLength, string propertyName = null)`
Adds a rule that fails if the length of `value` is less than `minLength` or greater than `maxLength`. Returns the same `ValidationHelper` instance for chaining. Errors are deferred.

### `public ValidationHelper RequireRange<T>(T value, T min, T max, string propertyName = null) where T : IComparable<T>`
Adds a rule that fails if `value` is less than `min` or greater than `max`. Returns the same `ValidationHelper` instance for chaining. Errors are collected.

### `public ValidationHelper RequireMinimumItems<T>(IEnumerable<T> collection, int minimum, string propertyName = null)`
Adds a rule that fails if `collection` contains fewer than `minimum` elements. Returns the same `ValidationHelper` instance for chaining. Errors are deferred.

### `public ValidationHelper RequireMaximumItems<T>(IEnumerable<T> collection, int maximum, string propertyName = null)`
Adds a rule that fails if `collection` contains more than `maximum` elements. Returns the same `ValidationHelper` instance for chaining. Errors are deferred.

### `public ValidationHelper RequireCondition(bool condition, string errorMessage, string propertyName = null)`
Adds a rule that fails when `condition` is `false`, associating the supplied `errorMessage` with the failure. Returns the same `ValidationHelper` instance for chaining. Errors are collected.

### `public ValidationHelper RequireValidEmail(string email, string propertyName = null)`
Adds a rule that fails if `email` does not conform to a basic email address format. Returns the same `ValidationHelper` instance for chaining. Errors are deferred.

### `public ValidationHelper RequireValidUrl(string url, string propertyName = null)`
Adds a rule that fails if `url` does not conform to a basic URL format. Returns the same `ValidationHelper` instance for chaining. Errors are deferred.

### `public void ThrowIfInvalid()`
If any validation rules have recorded errors, throws a `ValidationException` containing a concatenated message of all errors. If no errors are present, the method returns without throwing.

### `public IReadOnlyList<string> GetErrors()`
Returns a read‑only list of all error messages that have been recorded by the validation rules invoked on this instance. The list is empty if no rules have failed.

### `public ValidationException(string message) : base(message)`
Constructor for the exception type thrown by `ThrowIfInvalid`. Initializes the exception with the supplied `message`.

## Usage

```csharp
var helper = new ValidationHelper();

helper.RequireNotNull(model.Name, nameof(model.Name))
      .RequireNotEmpty(model.Email, nameof(model.Email))
      .RequireValidEmail(model.Email, nameof(model.Email))
      .RequireRange(model.Age, 18, 100, nameof(model.Age))
      .RequireCondition(model.TermsAccepted, "Terms must be accepted.", nameof(model.TermsAccepted));

if (helper.GetErrors().Any())
{
    // Handle validation failures without throwing
    foreach (var err in helper.GetErrors())
    {
        logger.Warning(err);
    }
}
else
{
    helper.ThrowIfInvalid(); // will not throw because no errors were recorded
}
```

```csharp
var items = GetItemsFromSource();

var validation = new ValidationHelper()
    .RequireNotNull(items, nameof(items))
    .RequireMinimumItems(items, 1, nameof(items))
    .RequireMaximumItems(items, 100, nameof(items));

try
{
    validation.ThrowIfInvalid();
}
catch (ValidationException ex)
{
    // ex.Message contains all validation problems
    BadRequest(ex.Message);
}
```

## Notes

- The `ValidationHelper` instance is mutable; each validation method appends internal state. Reusing the same instance across multiple unrelated validation sequences will cause errors from prior validations to persist unless a new instance is created.
- The helper is **not thread‑safe**. Concurrent calls to validation methods on the same instance from multiple threads may result in lost or duplicated error messages. For concurrent scenarios, create a separate `ValidationHelper` per thread or per validation operation.
- Validation methods do not throw exceptions for individual rule failures; they merely record the error. The only point at which an exception is thrown is `ThrowIfInvalid`, which aggregates all recorded messages into a single `ValidationException`.
- Passing `null` for the `value` argument to methods that expect a reference type (e.g., `RequireNotNull<T>`, `RequireNotEmpty`) will cause the corresponding rule to fail and an error to be recorded.
- The regular expression used by `RequirePattern` is not validated by the helper; supplying an malformed pattern will result in an exception from the .NET regex engine during evaluation.
- Email and URL validation use basic format checks; they do not guarantee deliverability or reachability. Adjust or replace these rules with stricter logic if required by the application.
