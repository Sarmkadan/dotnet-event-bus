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
/// Extension methods for HttpEventPublisher to provide fluent and convenient APIs.
/// </summary>
public static class HttpEventPublisherExtensions
{
    /// <summary>
    /// Publishes an event with custom headers and automatic JSON serialization.
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="url">The target URL</param>
    /// <param name="eventData">The event payload</param>
    /// <param name="configureHeaders">Optional action to configure request headers</param>
    /// <returns>Publish result</returns>
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

    /// <summary>
    /// Publishes an event with custom content type.
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="url">The target URL</param>
    /// <param name="eventData">The event payload</param>
    /// <param name="contentType">Custom content type (e.g., "application/xml")</param>
    /// <returns>Publish result</returns>
    public static async Task<HttpPublishResult> PublishAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData,
        string contentType)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);

        return await publisher.PublishAsync(url, eventData, null, contentType);
    }

    /// <summary>
    /// Publishes an event and returns true if the HTTP status code indicates success (2xx).
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="url">The target URL</param>
    /// <param name="eventData">The event payload</param>
    /// <returns>True if successful (2xx status code)</returns>
    public static async Task<bool> PublishSuccessfullyAsync(
        this HttpEventPublisher publisher,
        string url,
        object eventData)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);

        var result = await publisher.PublishAsync(url, eventData);
        return result.Success && result.StatusCode >= 200 && result.StatusCode < 300;
    }

    /// <summary>
    /// Publishes an event with custom headers and returns true if the HTTP status code indicates success (2xx).
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="url">The target URL</param>
    /// <param name="eventData">The event payload</param>
    /// <param name="customHeaders">Custom headers to add</param>
    /// <returns>True if successful (2xx status code)</returns>
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
        return result.Success && result.StatusCode >= 200 && result.StatusCode < 300;
    }

    /// <summary>
    /// Publishes an event with custom headers and returns a tuple with the result and response details.
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="url">The target URL</param>
    /// <param name="eventData">The event payload</param>
    /// <param name="customHeaders">Custom headers to add</param>
    /// <returns>Tuple containing success flag, status code, and error message (if any)</returns>
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
    /// Publishes an event with custom headers configured via a fluent action.
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="url">The target URL</param>
    /// <param name="eventData">The event payload</param>
    /// <param name="configureHeaders">Action to configure headers</param>
    /// <returns>Tuple containing success flag, status code, and error message (if any)</returns>
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
    /// Publishes multiple events sequentially and returns a collection of results.
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="publishRequests">Collection of publish requests</param>
    /// <returns>Collection of publish results</returns>
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
    /// Publishes multiple events with the same payload to different URLs.
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="urls">Collection of target URLs</param>
    /// <param name="eventData">The event payload</param>
    /// <returns>Collection of publish results</returns>
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
    /// Publishes an event and checks if the error message indicates a specific error type.
    /// </summary>
    /// <param name="publisher">The HttpEventPublisher instance</param>
    /// <param name="url">The target URL</param>
    /// <param name="eventData">The event payload</param>
    /// <param name="errorContains">String to search for in error message</param>
    /// <returns>True if error message contains the specified string</returns>
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
