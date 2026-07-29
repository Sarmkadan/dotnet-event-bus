#nullable enable

using System;
using DotnetEventBus.Exceptions;

namespace DotnetEventBus.Models;

/// <summary>
/// Extension methods for <see cref="PublishResult"/>.
/// </summary>
public static class PublishResultExtensions
{
    /// <summary>
    /// Executes one of the supplied delegates depending on whether the publish operation succeeded.
    /// </summary>
    /// <typeparam name="T">The return type of the delegates.</typeparam>
    /// <param name="result">The <see cref="PublishResult"/> instance.</param>
    /// <param name="onSuccess">Delegate to invoke when <see cref="PublishResult.Success"/> is true.</param>
    /// <param name="onFailure">Delegate to invoke when <see cref="PublishResult.Success"/> is false. The error message is passed as an argument.</param>
    /// <returns>The value returned by the selected delegate.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="result"/>, <paramref name="onSuccess"/> or <paramref name="onFailure"/> is null.</exception>
    public static T Match<T>(this PublishResult result, Func<T> onSuccess, Func<string, T> onFailure)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure is null) throw new ArgumentNullException(nameof(onFailure));

        return result.Success
            ? onSuccess()
            : onFailure(result.ErrorMessage ?? "Publish failed");
    }

    /// <summary>
    /// Throws an <see cref="EventBusException"/> if the publish operation failed.
    /// </summary>
    /// <param name="result">The <see cref="PublishResult"/> instance.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="result"/> is null.</exception>
    /// <exception cref="EventBusException">When <see cref="PublishResult.Success"/> is false.</exception>
    public static void ThrowIfFailed(this PublishResult result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));

        if (!result.Success)
        {
            var message = result.ErrorMessage ?? "Publish failed";
            throw new EventBusException(message);
        }
    }
}
