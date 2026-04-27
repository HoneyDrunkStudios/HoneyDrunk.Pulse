// <copyright file="PostHogSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Sink.PostHog.Mapping;
using HoneyDrunk.Telemetry.Sink.PostHog.Options;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace HoneyDrunk.Telemetry.Sink.PostHog.Implementation;

/// <summary>
/// HTTP-based PostHog analytics sink implementation.
/// </summary>
public sealed partial class PostHogSink : IAnalyticsSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly ISecretStore _secretStore;
    private readonly PostHogSinkOptions _options;
    private readonly PostHogEventMapper _mapper;
    private readonly ILogger<PostHogSink> _logger;
    private readonly SemaphoreSlim _batchLock = new(1, 1);
    private readonly List<TelemetryEvent> _pendingEvents = [];
    private readonly Timer? _flushTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostHogSink"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="secretStore">The Vault secret store.</param>
    /// <param name="options">The PostHog sink options.</param>
    /// <param name="logger">The logger.</param>
    public PostHogSink(
        HttpClient httpClient,
        ISecretStore secretStore,
        IOptions<PostHogSinkOptions> options,
        ILogger<PostHogSink> logger)
    {
        _httpClient = httpClient;
        _secretStore = secretStore;
        _options = options.Value;
        _logger = logger;
        _mapper = new PostHogEventMapper(_options);

        if (_options.Enabled && _options.FlushIntervalMs > 0)
        {
            _flushTimer = new Timer(
                async _ => await FlushInternalAsync().ConfigureAwait(false),
                null,
                TimeSpan.FromMilliseconds(_options.FlushIntervalMs),
                TimeSpan.FromMilliseconds(_options.FlushIntervalMs));
        }
    }

    /// <inheritdoc />
    public async Task CaptureAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await _batchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _pendingEvents.Add(telemetryEvent);

            if (_pendingEvents.Count >= _options.BatchSize)
            {
                await FlushInternalAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _batchLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CaptureBatchAsync(IEnumerable<TelemetryEvent> events, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await _batchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _pendingEvents.AddRange(events);

            if (_pendingEvents.Count >= _options.BatchSize)
            {
                await FlushInternalAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _batchLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _batchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await FlushInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _batchLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _flushTimer?.Dispose();
        _batchLock.Dispose();
        _disposed = true;
    }

    private async Task FlushInternalAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingEvents.Count == 0)
        {
            return;
        }

        var eventsToSend = _pendingEvents.ToList();
        _pendingEvents.Clear();

        try
        {
            var apiKey = await ResolveApiKeyAsync(cancellationToken).ConfigureAwait(false);
            var payload = _mapper.MapBatch(eventsToSend, apiKey);
            await SendBatchAsync(payload, cancellationToken).ConfigureAwait(false);

            LogEventsSent(eventsToSend.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSendFailed(ex, eventsToSend.Count);

            // Re-add events that failed to send
            _pendingEvents.InsertRange(0, eventsToSend);
        }
    }

    private async Task<string> ResolveApiKeyAsync(CancellationToken cancellationToken)
    {
        var secret = await _secretStore
            .GetSecretAsync(new SecretIdentifier(_options.ApiKeySecretName), cancellationToken)
            .ConfigureAwait(false);

        return secret.Value;
    }

    private async Task SendBatchAsync(PostHogBatchPayload payload, CancellationToken cancellationToken)
    {
        var endpoint = new Uri(new Uri(_options.Host), "/batch");

        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    payload,
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning(
                    "PostHog API returned {StatusCode}: {ResponseBody}",
                    response.StatusCode,
                    responseBody);

                // 429 Too Many Requests is retryable — respect Retry-After header
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt < _options.MaxRetries)
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta
                            ?? TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 1000);
                        await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw new HttpRequestException($"PostHog API rate limited after {_options.MaxRetries} retries");
                }

                // Don't retry on other client errors (4xx)
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    throw new HttpRequestException($"PostHog API error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException) when (attempt < _options.MaxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
