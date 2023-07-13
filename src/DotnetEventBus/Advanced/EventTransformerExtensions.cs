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
/// <remarks>
/// All methods validate arguments and throw <see cref="ArgumentNullException"/> for null inputs.
/// Methods use expression-bodied syntax where appropriate for idiomatic C#.
/// </remarks>
public static class EventTransformerExtensions
{
    /// <summary>
    /// Transforms the event and applies a conditional post-transformation if the condition is met.
    /// </summary>
    /// <typeparam name="TSource">Source event type</typeparam>
    /// <typeparam name="TTarget">Target event type</typeparam>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="condition">Function that determines if the post-transformation should be applied</param>
    /// <param name="postTransform">Transformation to apply when condition is <see langword="true"/></param>
    /// <returns>The transformer for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transformer"/>, <paramref name="condition"/>, or <paramref name="postTransform"/> is <see langword="null"/></exception>
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
    /// <typeparam name="TSource">Source event type</typeparam>
    /// <typeparam name="TTarget">Target event type</typeparam>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="postTransform">Transformation to apply when target is not null</param>
    /// <returns>The transformer for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transformer"/> or <paramref name="postTransform"/> is <see langword="null"/></exception>
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
    /// <typeparam name="TSource">Source event type</typeparam>
    /// <typeparam name="TTarget">Target event type</typeparam>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="postTransforms">Sequence of transformations to apply</param>
    /// <returns>The transformer for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transformer"/> or <paramref name="postTransforms"/> is <see langword="null"/></exception>
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
    /// <typeparam name="TSource">Source event type</typeparam>
    /// <typeparam name="TTarget">Target event type</typeparam>
    /// <typeparam name="TIntermediate">Intermediate transformation type</typeparam>
    /// <param name="transformer">The transformer instance</param>
    /// <param name="intermediateTransform">Transformation to apply to create intermediate type</param>
    /// <param name="finalTransform">Transformation to convert back to target type</param>
    /// <returns>The transformer for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="transformer"/>, <paramref name="intermediateTransform"/>, or <paramref name="finalTransform"/> is <see langword="null"/></exception>
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