// <copyright file="ExtractedErrorSpan.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Represents an error span extracted from OTLP trace data.
/// </summary>
/// <param name="SpanName">The name of the span.</param>
/// <param name="ServiceName">The service that generated the error.</param>
/// <param name="ErrorMessage">The error message or status description.</param>
/// <param name="ExceptionType">The exception type if available.</param>
/// <param name="ExceptionMessage">The exception message if available.</param>
/// <param name="StackTrace">The stack trace if available.</param>
/// <param name="TraceId">The trace ID for correlation.</param>
/// <param name="SpanId">The span ID.</param>
/// <param name="Attributes">Additional attributes from the span.</param>
public sealed record ExtractedErrorSpan(
    string SpanName,
    string? ServiceName,
    string? ErrorMessage,
    string? ExceptionType,
    string? ExceptionMessage,
    string? StackTrace,
    string? TraceId,
    string? SpanId,
    IReadOnlyDictionary<string, string> Attributes);
