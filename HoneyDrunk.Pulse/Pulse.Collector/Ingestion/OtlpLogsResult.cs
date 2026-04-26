// <copyright file="OtlpLogsResult.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Result from parsing an OTLP logs request.
/// </summary>
/// <param name="LogRecordCount">The number of log records in the request.</param>
/// <param name="ResourceNames">The service names from resource attributes.</param>
/// <param name="ErrorLogs">Error logs extracted for forwarding to error sinks.</param>
/// <param name="MaxSeverityNumber">The maximum severity number found in the batch (for log level filtering).</param>
public sealed record OtlpLogsResult(
    int LogRecordCount,
    IReadOnlyList<string> ResourceNames,
    IReadOnlyList<ExtractedErrorLog> ErrorLogs,
    int MaxSeverityNumber = 0)
{
    /// <summary>
    /// Gets an empty result.
    /// </summary>
    public static OtlpLogsResult Empty { get; } = new(0, [], [], 0);
}
