#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetEventBus.Integration;

/// <summary>
/// Provides extension methods for <see cref="HttpEventPublisher"/> to enable fluent and convenient APIs for event publishing.
/// </summary>
public static class HttpEventPublisherExtensions
{
    /// <summary>
    /// Publishes an event with custom headers configured via a fluent action and automatic JSON serialization.
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="eventData">The event payload.</param>
    /// <param name="configureHeaders">Optional action to configure request headers.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous publish operation. The task result contains the publish result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="url"/>, or <paramref name="eventData"/> is <see langword="null"/>.</exception>

    /// <summary>
    /// Publishes an event with a custom content type.
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="eventData">The event payload.</param>
    /// <param name="contentType">Custom content type (e.g., "application/xml").</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous publish operation. The task result contains the publish result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="url"/>, or <paramref name="eventData"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="contentType"/> is <see langword="null"/>.</exception>

    /// <summary>
    /// Publishes an event and returns <see langword="true"/> if the HTTP status code indicates success (2xx).
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="eventData">The event payload.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains <see langword="true"/> if the HTTP status code indicates success (2xx); otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="url"/>, or <paramref name="eventData"/> is <see langword="null"/>.</exception>
    public static async Task<bool> PublishSuccessfullyAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);

        var result = await publisher.PublishAsync(url, eventData);
        return result.Success && result.StatusCode is >= 200 and < 300;
    }

    public static async Task<HttpPublishResult> PublishAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData,
        Action<Dictionary<string, string>>? configureHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);

        var customHeaders = new Dictionary<string, string>();
        configureHeaders?.Invoke(customHeaders);

        return await publisher.PublishAsync(url, eventData, customHeaders);
    }

    public static async Task<HttpPublishResult> PublishAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData,
        string contentType)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(contentType);

        return await publisher.PublishAsync(url, eventData, null, contentType);
    }

    /// <summary>
    /// Publishes an event with custom headers and returns <see langword="true"/> if the HTTP status code indicates success (2xx).
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="eventData">The event payload.</param>
    /// <param name="customHeaders">Custom headers to add.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains <see langword="true"/> if the HTTP status code indicates success (2xx); otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="url"/>, <paramref name="eventData"/>, or <paramref name="customHeaders"/> is <see langword="null"/>.</exception>
    public static async Task<bool> PublishSuccessfullyAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData,
        Dictionary<string, string> customHeaders)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(customHeaders);

        var result = await publisher.PublishAsync(url, eventData, customHeaders);
        return result.Success && result.StatusCode is >= 200 and < 300;
    }

    /// <summary>
    /// Publishes an event with custom headers and returns a tuple containing the result and response details.
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="eventData">The event payload.</param>
    /// <param name="customHeaders">Custom headers to add (optional).</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains a tuple with:
    /// <list type="bullet">
    /// <item><see langword="Success"/> - <see langword="true"/> if the request was successful; otherwise, <see langword="false"/>.</item>
    /// <item><see langword="StatusCode"/> - The HTTP status code returned by the server.</item>
    /// <item><see langword="ErrorMessage"/> - The error message if the request failed; otherwise, <see langword="null"/>.</item>
    /// </list></returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="url"/>, or <paramref name="eventData"/> is <see langword="null"/>.</exception>
    public static async Task<(bool Success, int StatusCode, string? ErrorMessage)> PublishWithDetailsAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData,
        Dictionary<string, string>? customHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);

        var result = await publisher.PublishAsync(url, eventData, customHeaders);
        return (result.Success, result.StatusCode, result.ErrorMessage);
    }

    /// <summary>
    /// Publishes an event with custom headers configured via a fluent action and returns a tuple containing the result and response details.
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="eventData">The event payload.</param>
    /// <param name="configureHeaders">Action to configure headers.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains a tuple with:
    /// <list type="bullet">
    /// <item><see langword="Success"/> - <see langword="true"/> if the request was successful; otherwise, <see langword="false"/>.</item>
    /// <item><see langword="StatusCode"/> - The HTTP status code returned by the server.</item>
    /// <item><see langword="ErrorMessage"/> - The error message if the request failed; otherwise, <see langword="null"/>.</item>
    /// </list></returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="url"/>, <paramref name="eventData"/>, or <paramref name="configureHeaders"/> is <see langword="null"/>.</exception>
    public static async Task<(bool Success, int StatusCode, string? ErrorMessage)> PublishWithDetailsAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData,
        Action<Dictionary<string, string>> configureHeaders)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(configureHeaders);

        var result = await publisher.PublishAsync(url, eventData, configureHeaders);
        return (result.Success, result.StatusCode, result.ErrorMessage);
    }

    /// <summary>
    /// Publishes multiple events sequentially and returns a read-only collection of results.
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="publishRequests">Collection of publish requests.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains a read-only collection of <see cref="HttpPublishResult"/> objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/> or <paramref name="publishRequests"/> is <see langword="null"/>.</exception>
    public static async Task<IReadOnlyList<HttpPublishResult>> PublishBatchAsync(
        this HttpEventPublisher publisher,
        IReadOnlyList<(string Url, object Data, Dictionary<string, string>? Headers)> publishRequests)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(publishRequests);

        var results = new List<HttpPublishResult>(publishRequests.Count);
        foreach (var request in publishRequests)
        {
            var result = await publisher.PublishAsync(
                request.Url,
                request.Data,
                request.Headers);
            results.Add(result);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Publishes the same event payload to multiple target URLs and returns a read-only collection of results.
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="urls">Collection of target URLs.</param>
    /// <param name="eventData">The event payload.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains a read-only collection of <see cref="HttpPublishResult"/> objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="urls"/>, or <paramref name="eventData"/> is <see langword="null"/>.</exception>
    public static async Task<IReadOnlyList<HttpPublishResult>> PublishToMultipleAsync(
        this HttpEventPublisher publisher,
        IReadOnlyList<string> urls,
        object eventData)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(urls);
        ArgumentNullException.ThrowIfNull(eventData);

        var results = new List<HttpPublishResult>(urls.Count);
        foreach (var url in urls)
        {
            var result = await publisher.PublishAsync(url, eventData);
            results.Add(result);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Publishes an event and determines whether the error message contains a specific error type.
    /// </summary>
    /// <param name="publisher">The <see cref="HttpEventPublisher"/> instance.</param>
    /// <param name="url">The target URL.</param>
    /// <param name="eventData">The event payload.</param>
    /// <param name="errorContains">The string to search for in the error message.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains <see langword="true"/> if the error message contains the specified string and the request was not successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="publisher"/>, <paramref name="url"/>, <paramref name="eventData"/>, or <paramref name="errorContains"/> is <see langword="null"/>.</exception>
    public static async Task<bool> PublishWithErrorContainingAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData,
        string errorContains)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(errorContains);

        var result = await publisher.PublishAsync(url, eventData);
        return !result.Success && result.ErrorMessage?.Contains(errorContains) == true;
    }
}
