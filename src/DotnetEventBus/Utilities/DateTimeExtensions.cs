#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Diagnostics.CodeAnalysis;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Extension methods for <see cref="DateTime"/> operations.
/// Provides utilities for time calculations, formatting, and UTC conversions.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>The Unix timestamp in seconds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the resulting timestamp would be outside the valid range for Unix timestamps.</exception>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp in milliseconds.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>The Unix timestamp in milliseconds.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the resulting timestamp would be outside the valid range for Unix timestamps.</exception>
    public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Creates a DateTime from a Unix timestamp.
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in seconds.</param>
    /// <returns>A DateTime representing the timestamp.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the timestamp is outside the valid range for Unix timestamps.</exception>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
    }

    /// <summary>
    /// Creates a DateTime from a Unix timestamp in milliseconds.
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in milliseconds.</param>
    /// <returns>A DateTime representing the timestamp.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the timestamp is outside the valid range for Unix timestamps.</exception>
    public static DateTime FromUnixTimestampMilliseconds(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
    }

    /// <summary>
    /// Gets the date portion only (time set to 00:00:00).
    /// </summary>
    /// <param name="dateTime">The DateTime to extract date from.</param>
    /// <returns>A DateTime with time portion set to midnight.</returns>
    public static DateTime GetDateOnly(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Determines if a date is today.
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the date is today; otherwise, false.</returns>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today;
    }

    /// <summary>
    /// Determines if a date is tomorrow.
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the date is tomorrow; otherwise, false.</returns>
    public static bool IsTomorrow(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today.AddDays(1);
    }

    /// <summary>
    /// Determines if a date is yesterday.
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the date is yesterday; otherwise, false.</returns>
    public static bool IsYesterday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today.AddDays(-1);
    }

    /// <summary>
    /// Rounds a DateTime to the nearest specified interval.
    /// Example: new DateTime(2024, 5, 4, 14, 35, 47).Round(TimeSpan.FromMinutes(5))
    /// Returns: 2024-05-04 14:35:00
    /// </summary>
    /// <param name="dateTime">The DateTime to round.</param>
    /// <param name="interval">The time interval to round to.</param>
    /// <returns>A DateTime rounded to the nearest interval.</returns>
    /// <exception cref="ArgumentException">Thrown when interval is not positive.</exception>
    public static DateTime Round(this DateTime dateTime, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be positive", nameof(interval));

        var ticks = (long)Math.Round((double)dateTime.Ticks / interval.Ticks) * interval.Ticks;
        return new DateTime(ticks);
    }

    /// <summary>
    /// Truncates a DateTime to remove sub-second precision.
    /// </summary>
    /// <param name="dateTime">The DateTime to truncate.</param>
    /// <returns>A DateTime with milliseconds and ticks set to zero.</returns>
    public static DateTime TruncateMilliseconds(this DateTime dateTime)
    {
        return dateTime.AddMilliseconds(-dateTime.Millisecond);
    }

    /// <summary>
    /// Gets the start of the day (00:00:00).
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of day for.</param>
    /// <returns>A DateTime with time portion set to midnight.</returns>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999).
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of day for.</param>
    /// <returns>A DateTime with time portion set to end of day.</returns>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// Gets the start of the week (Monday).
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of week for.</param>
    /// <returns>A DateTime representing the start of the week (Monday).</returns>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        int daysToMonday = (int)dateTime.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToMonday < 0)
            daysToMonday += 7;

        return dateTime.Date.AddDays(-daysToMonday);
    }

    /// <summary>
    /// Gets the end of the week (Sunday).
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of week for.</param>
    /// <returns>A DateTime representing the end of the week (Sunday).</returns>
    public static DateTime EndOfWeek(this DateTime dateTime)
    {
        return dateTime.StartOfWeek().AddDays(7).AddMilliseconds(-1);
    }

    /// <summary>
    /// Gets the start of the month.
    /// </summary>
    /// <param name="dateTime">The DateTime to get start of month for.</param>
    /// <returns>A DateTime representing the first day of the month.</returns>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month.
    /// </summary>
    /// <param name="dateTime">The DateTime to get end of month for.</param>
    /// <returns>A DateTime representing the last day of the month at 23:59:59.999.</returns>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1).AddMonths(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// Formats a DateTime as an ISO 8601 string.
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>An ISO 8601 formatted string.</returns>
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToString("o");
    }

    /// <summary>
    /// Checks if a DateTime is in the past.
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the DateTime is in the past; otherwise, false.</returns>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    /// <param name="dateTime">The DateTime to check.</param>
    /// <returns>True if the DateTime is in the future; otherwise, false.</returns>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the number of days between two dates.
    /// </summary>
    /// <param name="from">The starting date.</param>
    /// <param name="to">The ending date.</param>
    /// <returns>The absolute number of days between the two dates.</returns>
    public static int DaysBetween(this DateTime from, DateTime to)
    {
        return (int)Math.Abs((to.Date - from.Date).TotalDays);
    }
}