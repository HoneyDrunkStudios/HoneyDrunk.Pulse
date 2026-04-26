// <copyright file="PulseAnalyticsEmitter.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace HoneyDrunk.Telemetry.OpenTelemetry;

/// <summary>
/// HTTP-based implementation of <see cref="IAnalyticsEmitter"/> that sends events to Pulse.Collector.
/// </summary>
/// <remarks>
/// This implementation provides resilient analytics emission with automatic retry
/// and graceful degradation when the collector is unavailable.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="PulseAnalyticsEmitter"/> class.
/// </remarks>
/// <param name="httpClientFactory">The HTTP client factory.</param>
/// <param name="options">The emitter options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class PulseAnalyticsEmitter(
    IHttpClientFactory httpClientFactory,
    IOptions<PulseAnalyticsEmitterOptions> options,
    ILogger<PulseAnalyticsEmitter> logger) : IAnalyticsEmitter
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(PulseAnalyticsEmitterOptions.HttpClientName);
    private readonly PulseAnalyticsEmitterOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task EmitAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default)
    {
        await EmitBatchAsync([telemetryEvent], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task EmitBatchAsync(IEnumerable<TelemetryEvent> events, CancellationToken cancellationToken = default)
    {
        var eventsList = events.ToList();
        if (eventsList.Count == 0)
        {
            return;
        }

        try
        {
            var request = new AnalyticsRequest
            {
                Events = [.. eventsList.Select(e => new AnalyticsEventDto
                {
                    EventName = e.EventName,
                    Timestamp = e.Timestamp,
                    DistinctId = e.DistinctId,
                    UserId = e.UserId,
                    SessionId = e.SessionId,
                    CorrelationId = e.CorrelationId,
                    NodeId = e.NodeId,
                    Environment = e.Environment,
                    Properties = e.Properties,
                })],
                SourceService = _options.ServiceName,
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/otlp/v1/analytics",
                request,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                LogAnalyticsSent(eventsList.Count);
            }
            else
            {
                LogAnalyticsSendFailed(response.StatusCode.ToString(), eventsList.Count);
            }
        }
        catch (HttpRequestException ex)
        {
            // Collector unavailable - log and continue (graceful degradation)
            LogCollectorUnavailable(ex, eventsList.Count);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown
            throw;
        }
        catch (Exception ex)
        {
            LogUnexpectedError(ex, eventsList.Count);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Analytics events sent: {EventCount}")]
    private partial void LogAnalyticsSent(int eventCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send analytics events. Status: {StatusCode}, Count: {EventCount}")]
    private partial void LogAnalyticsSendFailed(string statusCode, int eventCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Pulse.Collector unavailable, analytics events not sent. Count: {EventCount}")]
    private partial void LogCollectorUnavailable(Exception ex, int eventCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error sending analytics events. Count: {EventCount}")]
    private partial void LogUnexpectedError(Exception ex, int eventCount);

    private sealed class AnalyticsRequest
    {
        public required List<AnalyticsEventDto> Events { get; init; }

        public required string? SourceService { get; init; }
    }

    private sealed class AnalyticsEventDto
    {
        public required string EventName { get; init; }

        public DateTimeOffset Timestamp { get; init; }

        public string? DistinctId { get; init; }

        public string? UserId { get; init; }

        public string? SessionId { get; init; }

        public string? CorrelationId { get; init; }

        public string? NodeId { get; init; }

        public string? Environment { get; init; }

        public IReadOnlyDictionary<string, object?>? Properties { get; init; }
    }
}
