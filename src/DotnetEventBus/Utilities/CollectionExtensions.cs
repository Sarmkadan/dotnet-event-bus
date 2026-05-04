// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetEventBus.Utilities;

/// <summary>
/// Extension methods for collections and enumerables.
/// Provides batch processing, safe access, and transformation utilities.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Batches an enumerable into groups of specified size.
    /// Useful for processing large datasets in chunks.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));

        var batch = new List<T>(batchSize);

        foreach (var item in items)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Determines if a collection is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source == null || !source.Any();

    /// <summary>
    /// Safely returns the first element or a default value if the collection is empty.
    /// </summary>
    public static T? FirstOrDefault<T>(this IEnumerable<T>? source, T? defaultValue = default) =>
        source?.FirstOrDefault() ?? defaultValue;

    /// <summary>
    /// Executes an action for each element in the enumerable.
    /// Returns the enumerable unchanged for chaining.
    /// </summary>
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source)
        {
            action(item);
            yield return item;
        }
    }

    /// <summary>
    /// Executes an async action for each element.
    /// </summary>
    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source)
        {
            await action(item);
        }
    }

    /// <summary>
    /// Converts an enumerable to a dictionary, handling duplicate keys gracefully.
    /// </summary>
    public static Dictionary<TKey, TValue> ToDictionaryDistinct<TSource, TKey, TValue>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector) where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>();

        foreach (var item in source)
        {
            var key = keySelector(item);
            var value = valueSelector(item);

            // Only add if key doesn't exist (first occurrence wins)
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Groups items by key and returns them as a dictionary.
    /// </summary>
    public static Dictionary<TKey, List<T>> GroupByToDictionary<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        return source
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Determines if two collections contain the same elements regardless of order.
    /// </summary>
    public static bool SetEquals<T>(this IEnumerable<T>? first, IEnumerable<T>? second) =>
        new HashSet<T>(first ?? []).SetEquals(second ?? []);

    /// <summary>
    /// Returns distinct elements from a collection using a custom comparer.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            if (seen.Add(keySelector(item)))
                yield return item;
        }
    }

    /// <summary>
    /// Returns a random element from a collection.
    /// </summary>
    public static T? GetRandom<T>(this IEnumerable<T> source)
    {
        var list = source as List<T> ?? source.ToList();
        return list.Count == 0 ? default : list[new Random().Next(list.Count)];
    }

    /// <summary>
    /// Chunks collection into pages of specified size.
    /// </summary>
    public static IEnumerable<Page<T>> AsPages<T>(this IEnumerable<T> source, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0);

        var pageNumber = 1;
        var batch = source.Batch(pageSize);

        foreach (var items in batch)
        {
            yield return new Page<T>(pageNumber, pageSize, items.ToList());
            pageNumber++;
        }
    }
}

/// <summary>
/// Represents a page of items from a paginated collection.
/// </summary>
public class Page<T>
{
    public int PageNumber { get; }
    public int PageSize { get; }
    public List<T> Items { get; }

    public Page(int pageNumber, int pageSize, List<T> items)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        Items = items;
    }

    public int TotalItems => Items.Count;
}
