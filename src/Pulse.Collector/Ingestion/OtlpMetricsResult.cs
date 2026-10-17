// <copyright file="OtlpMetricsResult.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Result from parsing an OTLP metrics request.
/// </summary>
/// <param name="MetricCount">The number of metrics in the request.</param>
/// <param name="DataPointCount">The total number of data points.</param>
/// <param name="ResourceNames">The service names from resource attributes.</param>
public sealed record OtlpMetricsResult(int MetricCount, int DataPointCount, IReadOnlyList<string> ResourceNames)
{
    /// <summary>
    /// Gets an empty result.
    /// </summary>
    public static OtlpMetricsResult Empty { get; } = new(0, 0, []);
}
