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
        Message = "Error processing traces from {SourceName} ({ExceptionType})")]
    private partial void LogTraceProcessingError(string exceptionType, string? sourceName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Processed {MetricCount} metrics from {SourceName}")]
    private partial void LogMetricsProcessed(int metricCount, string sourceName);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Error processing metrics from {SourceName} ({ExceptionType})")]
    private partial void LogMetricProcessingError(string exceptionType, string? sourceName);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Processed {LogCount} logs from {SourceName}")]
    private partial void LogLogsProcessed(int logCount, string sourceName);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Error processing logs from {SourceName} ({ExceptionType})")]
    private partial void LogLogProcessingError(string exceptionType, string? sourceName);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Processed {EventCount} analytics events from {SourceName}")]
    private partial void LogAnalyticsEventsProcessed(int eventCount, string sourceName);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Error,
        Message = "Error processing analytics events from {SourceName} ({ExceptionType})")]
    private partial void LogAnalyticsProcessingError(string exceptionType, string? sourceName);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Processed error event")]
    private partial void LogErrorEventProcessed();

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "Error routing to Sentry sink ({ExceptionType})")]
    private partial void LogSentryRoutingError(string exceptionType);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Debug,
        Message = "Forwarded error span to Sentry: {SpanName} from {ServiceName}")]
    private partial void LogErrorSpanForwarded(string spanName, string serviceName);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message = "Failed to forward error span to Sentry ({ExceptionType})")]
    private partial void LogErrorSpanForwardingFailed(string exceptionType);

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
        Message = "Failed to forward error log to Sentry ({ExceptionType})")]
    private partial void LogErrorLogForwardingFailed(string exceptionType);

    [LoggerMessage(
        EventId = 16,
        Level = LogLevel.Warning,
        Message = "Failed to forward traces to trace sink (Tempo) ({ExceptionType})")]
    private partial void LogTraceSinkForwardingFailed(string exceptionType);

    [LoggerMessage(
        EventId = 17,
        Level = LogLevel.Warning,
        Message = "Failed to forward metrics to metrics sink (Mimir) ({ExceptionType})")]
    private partial void LogMetricsSinkForwardingFailed(string exceptionType);

    [LoggerMessage(
        EventId = 18,
        Level = LogLevel.Warning,
        Message = "Failed to forward logs to log sink (Loki) ({ExceptionType})")]
    private partial void LogLogsSinkForwardingFailed(string exceptionType);

    [LoggerMessage(
        EventId = 19,
        Level = LogLevel.Warning,
        Message = "Failed to forward analytics events to analytics sink (PostHog) ({ExceptionType})")]
    private partial void LogAnalyticsSinkForwardingFailed(string exceptionType);

    [LoggerMessage(
        EventId = 20,
        Level = LogLevel.Debug,
        Message = "Logs batch filtered by minimum log level (max severity {MaxSeverity} below configured minimum {MinimumLevel})")]
    private partial void LogLogsBatchFilteredByLevel(int maxSeverity, string minimumLevel);

    [LoggerMessage(
        EventId = 21,
        Level = LogLevel.Warning,
        Message = "Failed to publish ingestion event to Transport for source {SourceName} ({ExceptionType})")]
    private partial void LogTransportPublishFailed(string exceptionType, string? sourceName);
}
