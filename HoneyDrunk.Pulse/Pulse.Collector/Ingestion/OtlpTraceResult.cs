// <copyright file="OtlpTraceResult.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Result from parsing an OTLP trace request.
/// </summary>
/// <param name="SpanCount">The number of spans in the request.</param>
/// <param name="ResourceNames">The service names from resource attributes.</param>
/// <param name="ErrorSpans">Error spans extracted for forwarding to error sinks.</param>
public sealed record OtlpTraceResult(
    int SpanCount,
    IReadOnlyList<string> ResourceNames,
    IReadOnlyList<ExtractedErrorSpan> ErrorSpans)
{
    /// <summary>
    /// Gets an empty result.
    /// </summary>
    public static OtlpTraceResult Empty { get; } = new(0, [], []);
}
