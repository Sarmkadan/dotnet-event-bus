#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Extension methods for DateTime operations.
/// Provides utilities for time calculations, formatting, and UTC conversions.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp in milliseconds.
    /// </summary>
    public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Creates a DateTime from a Unix timestamp.
    /// </summary>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
    }

    /// <summary>
    /// Creates a DateTime from a Unix timestamp in milliseconds.
    /// </summary>
    public static DateTime FromUnixTimestampMilliseconds(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
    }

    /// <summary>
    /// Gets the date portion only (time set to 00:00:00).
    /// </summary>
    public static DateTime GetDateOnly(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Determines if a date is today.
    /// </summary>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today;
    }

    /// <summary>
    /// Determines if a date is tomorrow.
    /// </summary>
    public static bool IsTomorrow(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today.AddDays(1);
    }

    /// <summary>
    /// Determines if a date is yesterday.
    /// </summary>
    public static bool IsYesterday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today.AddDays(-1);
    }

    /// <summary>
    /// Rounds a DateTime to the nearest specified interval.
    /// Example: new DateTime(2024, 5, 4, 14, 35, 47).Round(TimeSpan.FromMinutes(5))
    /// Returns: 2024-05-04 14:35:00
    /// </summary>
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
    public static DateTime TruncateMilliseconds(this DateTime dateTime)
    {
        return dateTime.AddMilliseconds(-dateTime.Millisecond);
    }

    /// <summary>
    /// Gets the start of the day (00:00:00).
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day (23:59:59.999).
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// Gets the start of the week (Monday).
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime)
    {
        int daysToMonday = (int)dateTime.DayOfWeek - 1;
        if (daysToMonday < 0)
            daysToMonday = 6;

        return dateTime.Date.AddDays(-daysToMonday);
    }

    /// <summary>
    /// Gets the end of the week (Sunday).
    /// </summary>
    public static DateTime EndOfWeek(this DateTime dateTime)
    {
        return dateTime.StartOfWeek().AddDays(7).AddMilliseconds(-1);
    }

    /// <summary>
    /// Gets the start of the month.
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month.
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1).AddMonths(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// Formats a DateTime as an ISO 8601 string.
    /// </summary>
    public static string ToIso8601String(this DateTime dateTime)
    {
        return dateTime.ToString("o");
    }

    /// <summary>
    /// Checks if a DateTime is in the past.
    /// </summary>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a DateTime is in the future.
    /// </summary>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the number of days between two dates.
    /// </summary>
    public static int DaysBetween(this DateTime from, DateTime to)
    {
        return (int)Math.Abs((to.Date - from.Date).TotalDays);
    }
}
