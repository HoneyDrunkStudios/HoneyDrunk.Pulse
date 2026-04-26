// <copyright file="IngestionPipeline.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Pulse.Collector.Configuration;
using HoneyDrunk.Pulse.Collector.Enrichment;
using HoneyDrunk.Pulse.Collector.Telemetry;
using HoneyDrunk.Pulse.Collector.Transport;
using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Sink.Loki.Options;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Pipeline for processing ingested telemetry and routing to sinks.
/// Supports multiple sinks per signal type with failure isolation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IngestionPipeline"/> class.
/// </remarks>
/// <param name="enricher">The telemetry enricher for adding HoneyDrunk context.</param>
/// <param name="analyticsSink">The analytics sink.</param>
/// <param name="errorSink">The error sink.</param>
/// <param name="traceSinks">The trace sinks for OTLP forwarding.</param>
/// <param name="logSinks">The log sinks for OTLP forwarding.</param>
/// <param name="metricsSinks">The metrics sinks for OTLP forwarding.</param>
/// <param name="publisher">The Transport event publisher.</param>
/// <param name="options">The collector options.</param>
/// <param name="lokiOptions">The Loki sink options for log level filtering.</param>
/// <param name="logger">The logger.</param>
public sealed partial class IngestionPipeline(
    TelemetryEnricher enricher,
    IAnalyticsSink? analyticsSink,
    IErrorSink? errorSink,
    IEnumerable<ITraceSink> traceSinks,
    IEnumerable<ILogSink> logSinks,
    IEnumerable<IMetricsSink> metricsSinks,
    PulseIngestedPublisher publisher,
    IOptions<PulseCollectorOptions> options,
    IOptions<LokiSinkOptions>? lokiOptions,
    ILogger<IngestionPipeline> logger)
{
    /// <summary>
    /// OTLP severity number threshold mapping for Microsoft.Extensions.Logging.LogLevel.
    /// </summary>
    private static readonly Dictionary<Microsoft.Extensions.Logging.LogLevel, int> LogLevelToOtlpSeverity =
        new()
        {
            { Microsoft.Extensions.Logging.LogLevel.Trace, 1 },
            { Microsoft.Extensions.Logging.LogLevel.Debug, 5 },
            { Microsoft.Extensions.Logging.LogLevel.Information, 9 },
            { Microsoft.Extensions.Logging.LogLevel.Warning, 13 },
            { Microsoft.Extensions.Logging.LogLevel.Error, 17 },
            { Microsoft.Extensions.Logging.LogLevel.Critical, 21 },
            { Microsoft.Extensions.Logging.LogLevel.None, int.MaxValue },
        };

    private readonly PulseCollectorOptions _options = options.Value;
    private readonly LokiSinkOptions? _lokiOptions = lokiOptions?.Value;
    private readonly List<ITraceSink> _traceSinks = [.. traceSinks];
    private readonly List<ILogSink> _logSinks = [.. logSinks];
    private readonly List<IMetricsSink> _metricsSinks = [.. metricsSinks];

    /// <summary>
    /// Processes ingested traces.
    /// </summary>
    /// <param name="traceCount">The number of traces.</param>
    /// <param name="sourceName">The source service name.</param>
    /// <param name="sourceNodeId">The source node ID.</param>
    /// <param name="errorSpans">Error spans extracted from traces for forwarding to Sentry.</param>
    /// <param name="rawOtlpData">Raw OTLP protobuf data for forwarding to trace sinks.</param>
    /// <param name="contentType">Content type of the raw data (e.g., application/x-protobuf).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessTracesAsync(
        int traceCount,
        string? sourceName,
        string? sourceNodeId,
        IReadOnlyList<ExtractedErrorSpan>? errorSpans = null,
        ReadOnlyMemory<byte>? rawOtlpData = null,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = CollectorTelemetry.StartIngestionActivity("ProcessTraces");
        var stopwatch = Stopwatch.StartNew();
        var sinkFailures = 0;

        try
        {
            CollectorTelemetry.RecordTracesIngested(traceCount, sourceName);

            // Forward error spans to Sentry
            if (errorSpans != null && errorSpans.Count > 0 && errorSink != null)
            {
                await ForwardErrorSpansToSentryAsync(errorSpans, cancellationToken).ConfigureAwait(false);
            }

            // Forward raw OTLP data to all trace sinks (Tempo, AzureMonitor, etc.)
            if (rawOtlpData.HasValue && _traceSinks.Count > 0)
            {
                sinkFailures = await ExportToTraceSinksAsync(
                    rawOtlpData.Value,
                    contentType ?? "application/x-protobuf",
                    cancellationToken).ConfigureAwait(false);
            }

            LogTracesProcessed(traceCount, errorSpans?.Count ?? 0, sourceName ?? "unknown");

            var status = sinkFailures > 0
                ? Contracts.Events.IngestionStatus.PartialSuccess
                : Contracts.Events.IngestionStatus.Success;

            await PublishIngestionEventAsync(
                traceCount: traceCount,
                metricCount: 0,
                logCount: 0,
                analyticsEventCount: 0,
                sourceName,
                sourceNodeId,
                status,
                stopwatch.ElapsedMilliseconds,
                sinkFailures,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogTraceProcessingError(ex, sourceName);
            CollectorTelemetry.RecordError("trace_processing");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            CollectorTelemetry.RecordProcessingDuration(stopwatch.ElapsedMilliseconds, sourceName);
        }
    }

    /// <summary>
    /// Processes ingested metrics.
    /// </summary>
    /// <param name="metricCount">The number of metrics.</param>
    /// <param name="sourceName">The source service name.</param>
    /// <param name="sourceNodeId">The source node ID.</param>
    /// <param name="rawOtlpData">Raw OTLP protobuf data for forwarding to metrics sinks.</param>
    /// <param name="contentType">Content type of the raw data (e.g., application/x-protobuf).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessMetricsAsync(
        int metricCount,
        string? sourceName,
        string? sourceNodeId,
        ReadOnlyMemory<byte>? rawOtlpData = null,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = CollectorTelemetry.StartIngestionActivity("ProcessMetrics");
        var stopwatch = Stopwatch.StartNew();
        var sinkFailures = 0;

        try
        {
            CollectorTelemetry.RecordMetricsIngested(metricCount, sourceName);

            // Forward raw OTLP data to all metrics sinks (Mimir, AzureMonitor, etc.)
            if (rawOtlpData.HasValue && _metricsSinks.Count > 0)
            {
                sinkFailures = await ExportToMetricsSinksAsync(
                    rawOtlpData.Value,
                    contentType ?? "application/x-protobuf",
                    cancellationToken).ConfigureAwait(false);
            }

            LogMetricsProcessed(metricCount, sourceName ?? "unknown");

            var status = sinkFailures > 0
                ? Contracts.Events.IngestionStatus.PartialSuccess
                : Contracts.Events.IngestionStatus.Success;

            await PublishIngestionEventAsync(
                traceCount: 0,
                metricCount: metricCount,
                logCount: 0,
                analyticsEventCount: 0,
                sourceName,
                sourceNodeId,
                status,
                stopwatch.ElapsedMilliseconds,
                sinkFailures,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogMetricProcessingError(ex, sourceName);
            CollectorTelemetry.RecordError("metric_processing");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            CollectorTelemetry.RecordProcessingDuration(stopwatch.ElapsedMilliseconds, sourceName);
        }
    }

    /// <summary>
    /// Processes ingested logs.
    /// </summary>
    /// <param name="logCount">The number of logs.</param>
    /// <param name="sourceName">The source service name.</param>
    /// <param name="sourceNodeId">The source node ID.</param>
    /// <param name="errorLogs">Error logs extracted for forwarding to Sentry.</param>
    /// <param name="rawOtlpData">Raw OTLP protobuf data for forwarding to log sinks.</param>
    /// <param name="contentType">Content type of the raw data (e.g., application/x-protobuf).</param>
    /// <param name="maxSeverityNumber">The maximum severity number in the batch for log level filtering.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessLogsAsync(
        int logCount,
        string? sourceName,
        string? sourceNodeId,
        IReadOnlyList<ExtractedErrorLog>? errorLogs = null,
        ReadOnlyMemory<byte>? rawOtlpData = null,
        string? contentType = null,
        int maxSeverityNumber = 0,
        CancellationToken cancellationToken = default)
    {
        using var activity = CollectorTelemetry.StartIngestionActivity("ProcessLogs");
        var stopwatch = Stopwatch.StartNew();
        var sinkFailures = 0;

        try
        {
            CollectorTelemetry.RecordLogsIngested(logCount, sourceName);

            // Forward error logs to Sentry
            if (errorLogs != null && errorLogs.Count > 0 && errorSink != null)
            {
                await ForwardErrorLogsToSentryAsync(errorLogs, sourceName, cancellationToken).ConfigureAwait(false);
            }

            // Forward raw OTLP data to all log sinks (Loki, AzureMonitor, etc.)
            if (rawOtlpData.HasValue && _logSinks.Count > 0)
            {
                sinkFailures = await ExportToLogSinksAsync(
                    rawOtlpData.Value,
                    contentType ?? "application/x-protobuf",
                    maxSeverityNumber,
                    cancellationToken).ConfigureAwait(false);
            }

            LogLogsProcessedWithErrors(logCount, errorLogs?.Count ?? 0, sourceName ?? "unknown");

            var status = sinkFailures > 0
                ? Contracts.Events.IngestionStatus.PartialSuccess
                : Contracts.Events.IngestionStatus.Success;

            await PublishIngestionEventAsync(
                traceCount: 0,
                metricCount: 0,
                logCount: logCount,
                analyticsEventCount: 0,
                sourceName,
                sourceNodeId,
                status,
                stopwatch.ElapsedMilliseconds,
                sinkFailures,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogLogProcessingError(ex, sourceName);
            CollectorTelemetry.RecordError("log_processing");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            CollectorTelemetry.RecordProcessingDuration(stopwatch.ElapsedMilliseconds, sourceName);
        }
    }

    /// <summary>
    /// Processes analytics events and routes them to the PostHog sink.
    /// </summary>
    /// <param name="events">The analytics events.</param>
    /// <param name="sourceName">The source service name.</param>
    /// <param name="sourceNodeId">The source node ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessAnalyticsEventsAsync(
        IEnumerable<TelemetryEvent> events,
        string? sourceName,
        string? sourceNodeId,
        CancellationToken cancellationToken = default)
    {
        using var activity = CollectorTelemetry.StartIngestionActivity("ProcessAnalyticsEvents");
        var stopwatch = Stopwatch.StartNew();
        var eventList = events.ToList();
        var sinkFailures = 0;

        try
        {
            // Enrich analytics events with HoneyDrunk context
            foreach (var evt in eventList)
            {
                enricher.EnrichTelemetryEvent(evt, sourceName);
            }

            if (analyticsSink is not null && _options.EnablePostHogSink)
            {
                try
                {
                    await analyticsSink.CaptureBatchAsync(eventList, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogAnalyticsSinkForwardingFailed(ex);
                    sinkFailures++;
                }
            }

            CollectorTelemetry.RecordAnalyticsEventsIngested(eventList.Count, sourceName);

            LogAnalyticsEventsProcessed(eventList.Count, sourceName ?? "unknown");

            var status = sinkFailures > 0
                ? Contracts.Events.IngestionStatus.PartialSuccess
                : Contracts.Events.IngestionStatus.Success;

            await PublishIngestionEventAsync(
                traceCount: 0,
                metricCount: 0,
                logCount: 0,
                analyticsEventCount: eventList.Count,
                sourceName,
                sourceNodeId,
                status,
                stopwatch.ElapsedMilliseconds,
                sinkFailures,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogAnalyticsProcessingError(ex, sourceName);
            CollectorTelemetry.RecordError("analytics_processing");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            CollectorTelemetry.RecordProcessingDuration(stopwatch.ElapsedMilliseconds, sourceName);
        }
    }

    /// <summary>
    /// Processes an error event and routes it to the Sentry sink.
    /// </summary>
    /// <param name="errorEvent">The error event.</param>
    /// <param name="sourceName">The source service name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessErrorAsync(
        ErrorEvent errorEvent,
        string? sourceName = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = CollectorTelemetry.StartIngestionActivity("ProcessError");

        try
        {
            // Enrich error event with HoneyDrunk context
            enricher.EnrichErrorEvent(errorEvent, sourceName);

            if (errorSink is not null && _options.EnableSentrySink)
            {
                await errorSink.CaptureAsync(errorEvent, cancellationToken).ConfigureAwait(false);
            }

            LogErrorEventProcessed(errorEvent.Message ?? "Exception");
        }
        catch (Exception ex)
        {
            LogSentryRoutingError(ex);
            CollectorTelemetry.RecordError("sentry_routing");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            // Don't rethrow - we don't want sink failures to break ingestion
        }
    }

    private static TelemetryEventSeverity MapSeverityNumberToSeverity(int severityNumber)
    {
        // OTLP severity numbers: 1-4=TRACE, 5-8=DEBUG, 9-12=INFO, 13-16=WARN, 17-20=ERROR, 21-24=FATAL
        return severityNumber switch
        {
            >= 21 => TelemetryEventSeverity.Fatal,
            >= 17 => TelemetryEventSeverity.Error,
            >= 13 => TelemetryEventSeverity.Warning,
            >= 9 => TelemetryEventSeverity.Info,
            _ => TelemetryEventSeverity.Debug,
        };
    }

    /// <summary>
    /// Exports trace data to all registered trace sinks.
    /// </summary>
    /// <returns>The number of sinks that failed.</returns>
    private async Task<int> ExportToTraceSinksAsync(
        ReadOnlyMemory<byte> data,
        string contentType,
        CancellationToken cancellationToken)
    {
        var failures = 0;

        foreach (var sink in _traceSinks)
        {
            try
            {
                await sink.ExportAsync(data, contentType, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogTraceSinkForwardingFailed(ex);
                failures++;
            }
        }

        return failures;
    }

    /// <summary>
    /// Exports metrics data to all registered metrics sinks.
    /// </summary>
    /// <returns>The number of sinks that failed.</returns>
    private async Task<int> ExportToMetricsSinksAsync(
        ReadOnlyMemory<byte> data,
        string contentType,
        CancellationToken cancellationToken)
    {
        var failures = 0;

        foreach (var sink in _metricsSinks)
        {
            try
            {
                await sink.ExportAsync(data, contentType, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMetricsSinkForwardingFailed(ex);
                failures++;
            }
        }

        return failures;
    }

    /// <summary>
    /// Exports log data to all registered log sinks with minimum log level filtering.
    /// </summary>
    /// <returns>The number of sinks that failed.</returns>
    private async Task<int> ExportToLogSinksAsync(
        ReadOnlyMemory<byte> data,
        string contentType,
        int maxSeverityNumber,
        CancellationToken cancellationToken)
    {
        var failures = 0;

        foreach (var sink in _logSinks)
        {
            try
            {
                // Apply minimum log level filtering for Loki
                if (ShouldFilterLogSink(sink, maxSeverityNumber))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        LogLogsBatchFilteredByLevel(maxSeverityNumber, _lokiOptions!.MinimumLogLevel.ToString());
                    }

                    continue;
                }

                await sink.ExportAsync(data, contentType, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogLogsSinkForwardingFailed(ex);
                failures++;
            }
        }

        return failures;
    }

    /// <summary>
    /// Determines if a log sink should be filtered based on minimum log level.
    /// </summary>
    private bool ShouldFilterLogSink(ILogSink sink, int maxSeverityNumber)
    {
        // Only filter Loki sinks based on LokiSinkOptions.MinimumLogLevel
        if (_lokiOptions == null || sink.GetType().Name != "LokiSink")
        {
            return false;
        }

        var minSeverity = LogLevelToOtlpSeverity.TryGetValue(_lokiOptions.MinimumLogLevel, out var sev) ? sev : 0;

        // If no logs meet the minimum severity threshold, filter out the batch
        return maxSeverityNumber < minSeverity;
    }

    private async Task PublishIngestionEventAsync(
        int traceCount,
        int metricCount,
        int logCount,
        int analyticsEventCount,
        string? sourceName,
        string? sourceNodeId,
        Contracts.Events.IngestionStatus status,
        long processingDurationMs,
        int sinkFailureCount,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableTransportPublishing)
        {
            return;
        }

        // Create enrichment metadata for the ingestion event
        var metadata = enricher.CreateIngestionMetadata(sourceName);

        var ingestionEvent = new Contracts.Events.PulseIngested
        {
            BatchId = Guid.NewGuid().ToString("N"),
            SourceNodeName = sourceName,
            SourceNodeId = sourceNodeId,
            TraceCount = traceCount,
            MetricCount = metricCount,
            LogCount = logCount,
            AnalyticsEventCount = analyticsEventCount,
            Status = status,
            ProcessingDurationMs = processingDurationMs,
        };

        // Add sink failure info if applicable
        if (sinkFailureCount > 0)
        {
            ingestionEvent.ErrorMessage = $"{sinkFailureCount} sink(s) failed to export";
            metadata["pulse.sink_failures"] = sinkFailureCount.ToString(CultureInfo.InvariantCulture);
        }

        // Add enrichment metadata
        foreach (var kvp in metadata)
        {
            ingestionEvent.Metadata[kvp.Key] = kvp.Value;
        }

        try
        {
            await publisher.PublishAsync(ingestionEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogTransportPublishFailed(ex, sourceName);
            CollectorTelemetry.RecordError("transport_publish");
        }
    }

    /// <summary>
    /// Forwards extracted error spans to Sentry.
    /// </summary>
    private async Task ForwardErrorSpansToSentryAsync(
        IReadOnlyList<ExtractedErrorSpan> errorSpans,
        CancellationToken cancellationToken)
    {
        foreach (var errorSpan in errorSpans)
        {
            try
            {
                // Create error event for Sentry
                var errorEvent = new ErrorEvent
                {
                    Message = errorSpan.ErrorMessage
                        ?? errorSpan.ExceptionMessage
                        ?? $"Error in span: {errorSpan.SpanName}",
                    Severity = TelemetryEventSeverity.Error,
                    CorrelationId = errorSpan.TraceId,
                    OperationId = errorSpan.SpanId,
                    Timestamp = DateTimeOffset.UtcNow,
                };

                // Add tags
                errorEvent.Tags["span.name"] = errorSpan.SpanName;

                if (!string.IsNullOrEmpty(errorSpan.TraceId))
                {
                    errorEvent.Tags["trace.id"] = errorSpan.TraceId;
                }

                if (!string.IsNullOrEmpty(errorSpan.SpanId))
                {
                    errorEvent.Tags["span.id"] = errorSpan.SpanId;
                }

                // Add span attributes as tags
                foreach (var attr in errorSpan.Attributes)
                {
                    // Limit tag key length and sanitize
                    var key = attr.Key.Length > 32 ? attr.Key[..32] : attr.Key;
                    errorEvent.Tags[key] = attr.Value;
                }

                // Add exception details to extra data
                if (!string.IsNullOrEmpty(errorSpan.ExceptionType))
                {
                    errorEvent.Extra["exception.type"] = errorSpan.ExceptionType;
                }

                if (!string.IsNullOrEmpty(errorSpan.ExceptionMessage))
                {
                    errorEvent.Extra["exception.message"] = errorSpan.ExceptionMessage;
                }

                if (!string.IsNullOrEmpty(errorSpan.StackTrace))
                {
                    errorEvent.Extra["exception.stacktrace"] = errorSpan.StackTrace;
                }

                // Apply enrichment with HoneyDrunk context
                enricher.EnrichErrorEvent(errorEvent, errorSpan.ServiceName);

                await errorSink!.CaptureAsync(errorEvent, cancellationToken).ConfigureAwait(false);

                CollectorTelemetry.RecordErrorForwarded(errorSpan.ServiceName);

                LogErrorSpanForwarded(errorSpan.SpanName, errorSpan.ServiceName ?? "unknown");
            }
            catch (Exception ex)
            {
                LogErrorSpanForwardingFailed(ex, errorSpan.SpanName);
            }
        }
    }

    /// <summary>
    /// Forwards extracted error logs to Sentry.
    /// </summary>
    private async Task ForwardErrorLogsToSentryAsync(
        IReadOnlyList<ExtractedErrorLog> errorLogs,
        string? sourceName,
        CancellationToken cancellationToken)
    {
        foreach (var errorLog in errorLogs)
        {
            try
            {
                // Create error event for Sentry
                var errorEvent = new ErrorEvent
                {
                    Message = errorLog.Message ?? "Error log",
                    Severity = MapSeverityNumberToSeverity(errorLog.SeverityNumber),
                    Timestamp = errorLog.Timestamp,
                };

                // Add log attributes as tags
                if (!string.IsNullOrEmpty(errorLog.TraceId))
                {
                    errorEvent.Tags["trace.id"] = errorLog.TraceId;
                    errorEvent.CorrelationId = errorLog.TraceId;
                }

                if (!string.IsNullOrEmpty(errorLog.SpanId))
                {
                    errorEvent.Tags["span.id"] = errorLog.SpanId;
                    errorEvent.OperationId = errorLog.SpanId;
                }

                // Add severity info
                if (!string.IsNullOrEmpty(errorLog.SeverityText))
                {
                    errorEvent.Tags["severity.text"] = errorLog.SeverityText;
                }

                errorEvent.Tags["severity.number"] = errorLog.SeverityNumber.ToString(CultureInfo.InvariantCulture);

                // Add all log attributes
                foreach (var attr in errorLog.Attributes)
                {
                    var key = attr.Key.Length > 32 ? attr.Key[..32] : attr.Key;
                    errorEvent.Tags[key] = attr.Value;
                }

                // Add exception details if present
                if (!string.IsNullOrEmpty(errorLog.ExceptionType))
                {
                    errorEvent.Extra["exception.type"] = errorLog.ExceptionType;
                }

                if (!string.IsNullOrEmpty(errorLog.ExceptionMessage))
                {
                    errorEvent.Extra["exception.message"] = errorLog.ExceptionMessage;
                }

                if (!string.IsNullOrEmpty(errorLog.StackTrace))
                {
                    errorEvent.Extra["exception.stacktrace"] = errorLog.StackTrace;
                }

                // Apply enrichment with HoneyDrunk context
                enricher.EnrichErrorEvent(errorEvent, sourceName ?? errorLog.ServiceName);

                await errorSink!.CaptureAsync(errorEvent, cancellationToken).ConfigureAwait(false);

                CollectorTelemetry.RecordErrorForwarded(sourceName ?? errorLog.ServiceName);

                LogErrorLogForwarded(errorLog.SeverityText ?? "ERROR", sourceName ?? errorLog.ServiceName ?? "unknown");
            }
            catch (Exception ex)
            {
                LogErrorLogForwardingFailed(ex, errorLog.Message ?? "Error log");
            }
        }
    }
}
