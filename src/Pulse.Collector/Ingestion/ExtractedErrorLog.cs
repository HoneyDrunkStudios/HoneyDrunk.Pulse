// <copyright file="ExtractedErrorLog.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Represents an error log extracted from OTLP log data.
/// </summary>
/// <param name="Message">The log message body.</param>
/// <param name="SeverityNumber">The OTLP severity number (17-20=ERROR, 21-24=FATAL).</param>
/// <param name="SeverityText">The severity text (e.g., ERROR, FATAL).</param>
/// <param name="Timestamp">The timestamp of the log record.</param>
/// <param name="ServiceName">The service that generated the log.</param>
/// <param name="TraceId">The trace ID for correlation.</param>
/// <param name="SpanId">The span ID.</param>
/// <param name="ExceptionType">The exception type if available.</param>
/// <param name="ExceptionMessage">The exception message if available.</param>
/// <param name="StackTrace">The stack trace if available.</param>
/// <param name="Attributes">Additional attributes from the log record.</param>
public sealed record ExtractedErrorLog(
    string? Message,
    int SeverityNumber,
    string? SeverityText,
    DateTimeOffset Timestamp,
    string? ServiceName,
    string? TraceId,
    string? SpanId,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace,
    IReadOnlyDictionary<string, string> Attributes);
