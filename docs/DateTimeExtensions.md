# DateTimeExtensions

`DateTimeExtensions` is a static class that provides a set of extension methods for the `System.DateTime` type, enabling common date‑time conversions and comparisons such as Unix timestamp handling, date‑only extraction, relative day checks, and range calculations.

## API

### ToUnixTimestamp
- **Purpose**: Returns the number of whole seconds that have elapsed since the Unix epoch (1970‑01‑01T00:00:00Z) for the given `DateTime`.
- **Parameters**: `this DateTime value` – the date‑time to convert.
- **Return Value**: `long` – seconds since the Unix epoch.
- **Exceptions**: 
  - `ArgumentOutOfRangeException` if the resulting value is outside the range of a signed 64‑bit integer (practically only for dates far beyond year ±292 billion).

### ToUnixTimestampMilliseconds
- **Purpose**: Returns the number of whole milliseconds that have elapsed since the Unix epoch for the given `DateTime`.
- **Parameters**: `this DateTime value`.
- **Return Value**: `long` – milliseconds since the Unix epoch.
- **Exceptions**: Same as `ToUnixTimestamp` for extreme values.

### FromUnixTimestamp
- **Purpose**: Creates a `DateTime` representing the instant indicated by a Unix timestamp expressed in seconds.
- **Parameters**: `long unixTimeSeconds` – seconds since the Unix epoch.
- **Return Value**: `DateTime` – the corresponding UTC date‑time (`DateTimeKind.Utc`).
- **Exceptions**: 
  - `ArgumentOutOfRangeException` if `unixTimeSeconds` would produce a date outside the supported `DateTime` range.

### FromUnixTimestampMilliseconds
- **Purpose**: Creates a `DateTime` representing the instant indicated by a Unix timestamp expressed in milliseconds.
- **Parameters**: `long unixTimeMilliseconds`.
- **Return Value**: `DateTime` – UTC date‑time.
- **Exceptions**: Same as `FromUnixTimestamp`.

### GetDateOnly
- **Purpose**: Returns a new `DateTime` with the time component set to midnight (00:00:00) of the same day.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – same date, time set to 00:00:00, preserving the original `Kind`.
- **Exceptions**: None.

### IsToday
- **Purpose**: Determines whether the date component of the supplied `DateTime` falls on the current day (according to the local system time zone).
- **Parameters**: `this DateTime value`.
- **Return Value**: `bool` – `true` if the date is today; otherwise `false`.
- **Exceptions**: None.

### IsTomorrow
- **Purpose**: Determines whether the date component falls on the day after the current day.
- **Parameters**: `this DateTime value`.
- **Return Value**: `bool`.
- **Exceptions**: None.

### IsYesterday
- **Purpose**: Determines whether the date component falls on the day before the current day.
- **Parameters**: `this DateTime value`.
- **Return Value**: `bool`.
- **Exceptions**: None.

### Round
- **Purpose**: Rounds the `DateTime` to the nearest whole second (sub‑second fractions ≥ 0.5 s are rounded up).
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – rounded to the nearest second, preserving `Kind`.
- **Exceptions**: None.

### TruncateMilliseconds
- **Purpose**: Removes the millisecond and finer sub‑millisecond components, effectively truncating to whole milliseconds.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – same date‑time with milliseconds set to zero, preserving `Kind`.
- **Exceptions**: None.

### StartOfDay
- **Purpose**: Returns the `DateTime` representing 00:00:00 of the same day.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – start of the day, preserving `Kind`.
- **Exceptions**: None.

### EndOfDay
- **Purpose**: Returns the `DateTime` representing 23:59:59.9999999 of the same day.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – end of the day, preserving `Kind`.
- **Exceptions**: None.

### StartOfWeek
- **Purpose**: Returns the `DateTime` representing the first day of the week (Monday) at 00:00:00 for the week containing the supplied date.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – start of the week, preserving `Kind`.
- **Exceptions**: None.

### EndOfWeek
- **Purpose**: Returns the `DateTime` representing the last day of the week (Sunday) at 23:59:59.9999999 for the week containing the supplied date.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – end of the week, preserving `Kind`.
- **Exceptions**: None.

### StartOfMonth
- **Purpose**: Returns the `DateTime` representing the first day of the month at 00:00:00.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – start of the month, preserving `Kind`.
- **Exceptions**: None.

### EndOfMonth
- **Purpose**: Returns the `DateTime` representing the last day of the month at 23:59:59.9999999.
- **Parameters**: `this DateTime value`.
- **Return Value**: `DateTime` – end of the month, preserving `Kind`.
- **Exceptions**: None.

### ToIso8601String
- **Purpose**: Formats the `DateTime` as an ISO 8601 compliant string (`yyyy-MM-ddTHH:mm:ss.fffffffK`).
- **Parameters**: `this DateTime value`.
- **Return Value**: `string` – ISO 8601 representation.
- **Exceptions**: None.

### IsPast
- **Purpose**: Determines whether the `DateTime` is earlier than the current moment (`DateTime.Now`).
- **Parameters**: `this DateTime value`.
- **Return Value**: `bool` – `true` if the value is in the past; otherwise `false`.
- **Exceptions**: None.

### IsFuture
- **Purpose**: Determines whether the `DateTime` is later than the current moment (`DateTime.Now`).
- **Parameters**: `this DateTime value`.
- **Return Value**: `bool` – `true` if the value is in the future; otherwise `false`.
- **Exceptions**: None.

### DaysBetween
- **Purpose**: Calculates the number of whole days between two dates (ignoring time of day).
- **Parameters**: 
  - `this DateTime start` – the start date.
  - `DateTime end` – the end date.
- **Return Value**: `int` – signed count of days; negative if `end` precedes `start`.
- **Exceptions**: None.

## Usage

```csharp
using System;
using DotNetEventBus.Extensions; // assuming the namespace

class Program
{
    static void Main()
    {
        DateTime now = DateTime.UtcNow;

        // Convert to Unix timestamp (seconds) and back
        long seconds = now.ToUnixTimestamp();
        DateTime roundtrip = DateTimeExtensions.FromUnixTimestamp(seconds);
        Console.WriteLine($"Now: {now:O}");
        Console.WriteLine($"Unix seconds: {seconds}");
        Console.WriteLine($"Round‑trip: {roundtrip:O}");

        // Check if a date is today and calculate days until next month
        DateTime someDay = new DateTime(2025, 4, 15, 10, 30, 0, DateTimeKind.Utc);
        bool today = someDay.IsToday();
        int daysToMonthEnd = someDay.DaysBetween(someDay.EndOfMonth());
        Console.WriteLine($"{someDay:d} is today? {today}");
        Console.WriteLine($"Days to end of month: {daysToMonthEnd}");
    }
}
```

```csharp
using System;
using DotNetEventBus.Extensions;

class LoggingExample
{
    static void LogUtc(DateTime utcTime)
    {
        // Produce an ISO‑8601 string for log storage
        string iso = utcTime.ToIso8601String();
        Console.WriteLine($"[LOG] {iso}");

        // Truncate sub‑millisecond noise before comparison
        DateTime truncated = utcTime.TruncateMilliseconds();
        bool isRecent = truncated.IsPast && (DateTime.UtcNow - truncated).TotalSeconds < 5;
        Console.WriteLine($"Is recent (within 5 s)? {isRecent}");
    }
}
```

## Notes

- All extension methods are **pure**: they do not modify the input `DateTime` instance and rely only on its value, making them inherently thread‑safe.
- The methods that depend on the current system date/time (`IsToday`, `IsTomorrow`, `IsYesterday`, `IsPast`, `IsFuture`, `StartOfWeek`, `EndOfWeek`) use `DateTime.Now` (local time zone). If consistent UTC‑based checks are required, convert the input to UTC first (`value.ToUniversalTime()`).
- Conversion to/from Unix timestamps assumes the input represents an instant in UTC. If the `DateTime` has `Kind.Local` or `Kind.Unspecified`, the methods treat it as UTC by applying `ToUniversalTime()` internally; this may shift the result for local times.
- `Round` and `TruncateMilliseconds` operate on the tick level; `Round` follows the conventional “round half up” rule for sub‑second fractions.
- The `DaysBetween` method calculates the difference in whole days by comparing the date components only; any time‑of‑day information is discarded.
- No exceptions are thrown for typical values; extreme values that would cause the resulting `DateTime` to fall outside the supported range (`DateTime.MinValue`/`DateTime.MaxValue`) will raise `ArgumentOutOfRangeException`.
