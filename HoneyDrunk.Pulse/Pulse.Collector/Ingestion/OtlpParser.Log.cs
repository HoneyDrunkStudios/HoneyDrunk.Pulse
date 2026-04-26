// <copyright file="OtlpParser.Log.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// LoggerMessage source-generated logging methods for OtlpParser.
/// </summary>
public sealed partial class OtlpParser
{
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Debug,
        Message = "Parsed OTLP traces: ~{SpanCount} spans, {ErrorCount} errors from {ByteCount} bytes")]
    private partial void LogTracesParsed(int spanCount, int errorCount, int byteCount);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Error,
        Message = "Failed to parse OTLP trace request")]
    private partial void LogTraceParseError(Exception ex);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Debug,
        Message = "Parsed OTLP metrics: ~{MetricCount} metrics estimated from {ByteCount} bytes")]
    private partial void LogMetricsParsed(int metricCount, int byteCount);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Error,
        Message = "Failed to parse OTLP metrics request")]
    private partial void LogMetricParseError(Exception ex);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Debug,
        Message = "Parsed OTLP logs: ~{LogCount} log records estimated from {ByteCount} bytes")]
    private partial void LogLogsParsed(int logCount, int byteCount);

    [LoggerMessage(
        EventId = 105,
        Level = LogLevel.Error,
        Message = "Failed to parse OTLP logs request")]
    private partial void LogLogParseError(Exception ex);

    [LoggerMessage(
        EventId = 106,
        Level = LogLevel.Debug,
        Message = "Failed to parse OTLP JSON spans, falling back to heuristic")]
    private partial void LogJsonSpanParseFallback(Exception ex);

    [LoggerMessage(
        EventId = 107,
        Level = LogLevel.Debug,
        Message = "Failed to parse OTLP JSON metrics, falling back to heuristic")]
    private partial void LogJsonMetricParseFallback(Exception ex);

    [LoggerMessage(
        EventId = 108,
        Level = LogLevel.Debug,
        Message = "Failed to parse OTLP JSON logs, falling back to heuristic")]
    private partial void LogJsonLogParseFallback(Exception ex);

    [LoggerMessage(
        EventId = 109,
        Level = LogLevel.Debug,
        Message = "Failed to extract service names from OTLP JSON")]
    private partial void LogServiceNameExtractionFailed(Exception ex);

    [LoggerMessage(
        EventId = 110,
        Level = LogLevel.Debug,
        Message = "Failed to extract error spans from OTLP JSON")]
    private partial void LogErrorSpanExtractionFailed(Exception ex);

    [LoggerMessage(
        EventId = 111,
        Level = LogLevel.Debug,
        Message = "Parsed OTLP logs: ~{LogCount} log records, {ErrorLogCount} error logs from {ByteCount} bytes")]
    private partial void LogLogsParsedWithErrors(int logCount, int errorLogCount, int byteCount);

    [LoggerMessage(
        EventId = 112,
        Level = LogLevel.Error,
        Message = "Failed to parse OTLP traces from protobuf")]
    private partial void LogProtobufTraceParseError(Exception ex);

    [LoggerMessage(
        EventId = 113,
        Level = LogLevel.Error,
        Message = "Failed to parse OTLP logs from protobuf")]
    private partial void LogProtobufLogParseError(Exception ex);

    [LoggerMessage(
        EventId = 114,
        Level = LogLevel.Debug,
        Message = "Failed to extract error logs")]
    private partial void LogErrorLogExtractionFailed(Exception ex);

    [LoggerMessage(
        EventId = 115,
        Level = LogLevel.Debug,
        Message = "Extracted {ErrorSpanCount} error spans from protobuf traces")]
    private partial void LogErrorSpansExtractedFromProtobuf(int errorSpanCount);

    [LoggerMessage(
        EventId = 116,
        Level = LogLevel.Debug,
        Message = "Extracted {ErrorLogCount} error logs from protobuf logs")]
    private partial void LogErrorLogsExtractedFromProtobuf(int errorLogCount);
}
