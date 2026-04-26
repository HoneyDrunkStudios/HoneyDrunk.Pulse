// <copyright file="IngestionPipeline.Log.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// LoggerMessage source-generated logging methods for IngestionPipeline.
/// </summary>
public sealed partial class IngestionPipeline
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Processed {TraceCount} traces ({ErrorCount} errors) from {SourceName}")]
    private partial void LogTracesProcessed(int traceCount, int errorCount, string sourceName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Error processing traces from {SourceName}")]
    private partial void LogTraceProcessingError(Exception ex, string? sourceName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Processed {MetricCount} metrics from {SourceName}")]
    private partial void LogMetricsProcessed(int metricCount, string sourceName);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Error processing metrics from {SourceName}")]
    private partial void LogMetricProcessingError(Exception ex, string? sourceName);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Processed {LogCount} logs from {SourceName}")]
    private partial void LogLogsProcessed(int logCount, string sourceName);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Error processing logs from {SourceName}")]
    private partial void LogLogProcessingError(Exception ex, string? sourceName);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Processed {EventCount} analytics events from {SourceName}")]
    private partial void LogAnalyticsEventsProcessed(int eventCount, string sourceName);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Error processing analytics events from {SourceName}")]
    private partial void LogAnalyticsProcessingError(Exception ex, string? sourceName);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Processed error event: {Message}")]
    private partial void LogErrorEventProcessed(string message);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "Error routing to Sentry sink")]
    private partial void LogSentryRoutingError(Exception ex);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Debug,
        Message = "Forwarded error span to Sentry: {SpanName} from {ServiceName}")]
    private partial void LogErrorSpanForwarded(string spanName, string serviceName);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message = "Failed to forward error span to Sentry: {SpanName}")]
    private partial void LogErrorSpanForwardingFailed(Exception ex, string spanName);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Debug,
        Message = "Processed {LogCount} logs ({ErrorLogCount} error logs) from {SourceName}")]
    private partial void LogLogsProcessedWithErrors(int logCount, int errorLogCount, string sourceName);

    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Debug,
        Message = "Forwarded error log to Sentry: {Message} from {ServiceName}")]
    private partial void LogErrorLogForwarded(string message, string serviceName);

    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Warning,
        Message = "Failed to forward error log to Sentry: {Message}")]
    private partial void LogErrorLogForwardingFailed(Exception ex, string message);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Warning,
        Message = "Failed to forward traces to trace sink (Tempo)")]
    private partial void LogTraceSinkForwardingFailed(Exception ex);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Warning,
        Message = "Failed to forward metrics to metrics sink (Mimir)")]
    private partial void LogMetricsSinkForwardingFailed(Exception ex);

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Warning,
        Message = "Failed to forward logs to log sink (Loki)")]
    private partial void LogLogsSinkForwardingFailed(Exception ex);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Warning,
        Message = "Failed to forward analytics events to analytics sink (PostHog)")]
    private partial void LogAnalyticsSinkForwardingFailed(Exception ex);

    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Debug,
        Message = "Logs batch filtered by minimum log level (max severity {MaxSeverity} below configured minimum {MinimumLevel})")]
    private partial void LogLogsBatchFilteredByLevel(int maxSeverity, string minimumLevel);

    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Warning,
        Message = "Failed to publish ingestion event to Transport for source {SourceName}")]
    private partial void LogTransportPublishFailed(Exception ex, string? sourceName);
}
