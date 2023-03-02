#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetEventBus.Integration;

/// <summary>
/// Publishes events to remote HTTP endpoints.
/// Supports retry logic, timeouts, and header customization.
/// Why: Enables integration with external services and microservices over HTTP.
/// </summary>
public sealed class HttpEventPublisher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpEventPublisher> _logger;
    private readonly HttpEventPublisherOptions _options;

    public HttpEventPublisher(
        HttpClient httpClient,
        ILogger<HttpEventPublisher> logger,
        HttpEventPublisherOptions? options = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new HttpEventPublisherOptions();
    }

    /// <summary>
    /// Publishes an event to a remote HTTP endpoint.
    /// Automatically retries on failure with exponential backoff.
    /// </summary>
    public async Task<HttpPublishResult> PublishAsync(
        string url,
        object eventData,
        Dictionary<string, string>? customHeaders = null,
        string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(eventData);

        for (int attempt = 0; attempt < _options.MaxRetries; attempt++)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(eventData),
                    Encoding.UTF8,
                    contentType ?? "application/json");

                // Add custom headers
                if (customHeaders is not null)
                {
                    foreach (var header in customHeaders)
                    {
                        content.Headers.Add(header.Key, header.Value);
                    }
                }

                // Add correlation ID
                if (!string.IsNullOrEmpty(_options.CorrelationIdHeaderName))
                {
                    var correlationId = Guid.NewGuid().ToString();
                    _httpClient.DefaultRequestHeaders.Add(_options.CorrelationIdHeaderName, correlationId);
                }

                using (var cts = new System.Threading.CancellationTokenSource(_options.Timeout))
                {
                    var response = await _httpClient.PostAsync(url, content, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Event published successfully to {Url}", url);
                        return new HttpPublishResult(true, (int)response.StatusCode, null);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Event publish failed with status {StatusCode}: {Response}",
                        response.StatusCode, responseContent);

                    if (!ShouldRetry((int)response.StatusCode) || attempt == _options.MaxRetries - 1)
                    {
                        return new HttpPublishResult(false, (int)response.StatusCode, responseContent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event publish attempt {Attempt}/{MaxRetries} failed", attempt + 1, _options.MaxRetries);

                if (attempt == _options.MaxRetries - 1)
                {
                    return new HttpPublishResult(false, 0, ex.Message);
                }
            }

            // Exponential backoff
            if (attempt < _options.MaxRetries - 1)
            {
                var delay = _options.RetryDelayMs * (int)Math.Pow(2, attempt);
                await Task.Delay(delay);
            }
        }

        return new HttpPublishResult(false, 0, "All retry attempts exhausted");
    }

    private bool ShouldRetry(int statusCode)
    {
        // Retry on temporary errors (5xx) and timeout-like errors
        return statusCode >= 500 || statusCode == 408;
    }
}

public sealed class HttpEventPublisherOptions
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public string? CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";
}

public sealed class HttpPublishResult
{
    public bool Success { get; }
    public int StatusCode { get; }
    public string? ErrorMessage { get; }

    public HttpPublishResult(bool success, int statusCode, string? errorMessage)
    {
        Success = success;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
    }
}
