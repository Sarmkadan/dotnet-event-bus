#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Transforms events from one type to another using mapping functions.
/// Supports fluent transformation chains and composition.
/// Why: Allows handlers to receive events in a format optimized for their needs.
/// </summary>
public sealed class EventTransformer<TSource, TTarget> where TSource : class where TTarget : class
{
    private readonly Func<TSource, TTarget> _transformFunc;
    private readonly List<Func<TTarget, TTarget>> _postTransforms = [];

    public EventTransformer(Func<TSource, TTarget> transformFunc)
    {
        _transformFunc = transformFunc ?? throw new ArgumentNullException(nameof(transformFunc));
    }

    /// <summary>
    /// Adds a post-transformation step.
    /// </summary>
    public EventTransformer<TSource, TTarget> Then(Func<TTarget, TTarget> postTransform)
    {
        ArgumentNullException.ThrowIfNull(postTransform);
        _postTransforms.Add(postTransform);
        return this;
    }

    /// <summary>
    /// Transforms an event.
    /// </summary>
    public TTarget Transform(TSource sourceEvent)
    {
        ArgumentNullException.ThrowIfNull(sourceEvent);

        var result = _transformFunc(sourceEvent);

        // Apply post-transforms
        foreach (var postTransform in _postTransforms)
        {
            result = postTransform(result);
        }

        return result;
    }

    /// <summary>
    /// Transforms multiple events.
    /// </summary>
    public IEnumerable<TTarget> TransformMany(IEnumerable<TSource> sourceEvents)
    {
        return sourceEvents.Select(Transform);
    }

    /// <summary>
    /// Creates a chained transformer that applies multiple transformations.
    /// </summary>
    public EventTransformer<TSource, TIntermediate> Chain<TIntermediate>(
        Func<TTarget, TIntermediate> chainTransform) where TIntermediate : class
    {
        return new EventTransformer<TSource, TIntermediate>(source =>
        {
            var intermediate = Transform(source);
            return chainTransform(intermediate);
        });
    }
}

/// <summary>
/// Builder for creating event transformers fluently.
/// </summary>
public sealed class EventTransformerBuilder
{
    /// <summary>
    /// Creates a transformer from source to target type.
    /// </summary>
    public static EventTransformer<TSource, TTarget> CreateTransformer<TSource, TTarget>(
        Func<TSource, TTarget> mapFunc) where TSource : class where TTarget : class
    {
        return new EventTransformer<TSource, TTarget>(mapFunc);
    }

    /// <summary>
    /// Creates a transformer that copies common properties.
    /// </summary>
    public static EventTransformer<TSource, TTarget> CreatePropertyCopyTransformer<TSource, TTarget>()
        where TSource : class where TTarget : class, new()
    {
        return new EventTransformer<TSource, TTarget>(source =>
        {
            var target = new TTarget();
            var sourceProps = typeof(TSource).GetProperties();
            var targetProps = typeof(TTarget).GetProperties();

            foreach (var sourceProp in sourceProps)
            {
                var targetProp = targetProps.FirstOrDefault(p => p.Name == sourceProp.Name && p.CanWrite);
                if (targetProp is not null)
                {
                    try
                    {
                        var value = sourceProp.GetValue(source);
                        targetProp.SetValue(target, value);
                    }
                    catch
                    {
                        // Skip properties that can't be copied
                    }
                }
            }

            return target;
        });
    }

    /// <summary>
    /// Creates a transformer that converts to a dictionary.
    /// </summary>
    public static EventTransformer<T, Dictionary<string, object?>> CreateDictionaryTransformer<T>() where T : class
    {
        return new EventTransformer<T, Dictionary<string, object?>>(source =>
        {
            var dict = new Dictionary<string, object?>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                dict[prop.Name] = prop.GetValue(source);
            }

            return dict;
        });
    }
}
