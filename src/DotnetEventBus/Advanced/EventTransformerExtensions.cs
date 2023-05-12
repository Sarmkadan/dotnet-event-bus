#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetEventBus.Advanced;

/// <summary>
/// Extension methods for <see cref="EventTransformer{TSource, TTarget}"/> providing common transformation patterns.
/// </summary>
public static class EventTransformerExtensions
{
    /// <summary>
    /// Transforms the event and applies a conditional post-transformation if the condition is met.
    /// </summary>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="condition">Function that determines if the post-transformation should be applied</param>
    /// <param name="postTransform">Transformation to apply when condition is true</param>
    /// <returns>The transformer for fluent chaining</returns>
    public static EventTransformer<TSource, TTarget> When<TSource, TTarget>(
        this EventTransformer<TSource, TTarget> transformer,
        Func<TTarget, bool> condition,
        Func<TTarget, TTarget> postTransform)
        where TSource : class
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(transformer);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(postTransform);

        return transformer.Then(target => condition(target) ? postTransform(target) : target);
    }

    /// <summary>
    /// Transforms the event and applies a transformation only if the target is not null.
    /// </summary>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="postTransform">Transformation to apply when target is not null</param>
    /// <returns>The transformer for fluent chaining</returns>
    public static EventTransformer<TSource, TTarget> IfNotNull<TSource, TTarget>(
        this EventTransformer<TSource, TTarget> transformer,
        Func<TTarget, TTarget> postTransform)
        where TSource : class
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(transformer);
        ArgumentNullException.ThrowIfNull(postTransform);

        return transformer.Then(target => target is not null ? postTransform(target) : target);
    }

    /// <summary>
    /// Transforms the event and applies multiple post-transformations in sequence.
    /// </summary>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="postTransforms">Sequence of transformations to apply</param>
    /// <returns>The transformer for fluent chaining</returns>
    public static EventTransformer<TSource, TTarget> ThenAll<TSource, TTarget>(
        this EventTransformer<TSource, TTarget> transformer,
        params Func<TTarget, TTarget>[] postTransforms)
        where TSource : class
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(transformer);
        ArgumentNullException.ThrowIfNull(postTransforms);

        if (postTransforms.Length == 0)
        {
            return transformer;
        }

        return transformer.Then(target =>
        {
            var result = target;
            foreach (var postTransform in postTransforms)
            {
                result = postTransform(result);
            }
            return result;
        });
    }

    /// <summary>
    /// Transforms the event and applies a transformation that maps to a different type,
    /// then converts back to the original target type.
    /// </summary>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="intermediateTransform">Transformation to apply to create intermediate type</param>
    /// <param name="finalTransform">Transformation to convert back to target type</param>
    /// <returns>The transformer for fluent chaining</returns>
    public static EventTransformer<TSource, TTarget> MapIntermediate<TSource, TTarget, TIntermediate>(
        this EventTransformer<TSource, TTarget> transformer,
        Func<TTarget, TIntermediate> intermediateTransform,
        Func<TIntermediate, TTarget> finalTransform)
        where TSource : class
        where TTarget : class
        where TIntermediate : class
    {
        ArgumentNullException.ThrowIfNull(transformer);
        ArgumentNullException.ThrowIfNull(intermediateTransform);
        ArgumentNullException.ThrowIfNull(finalTransform);

        return transformer.Then(target => finalTransform(intermediateTransform(target)));
    }
}