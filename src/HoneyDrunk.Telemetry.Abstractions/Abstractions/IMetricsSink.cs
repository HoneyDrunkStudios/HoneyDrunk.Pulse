// <copyright file="IMetricsSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Abstractions;

/// <summary>
/// Defines the contract for a metrics sink that exports metric data.
/// </summary>
public interface IMetricsSink
{
    /// <summary>
    /// Exports metric data (OTLP format).
    /// </summary>
    /// <param name="metricData">The metric data as OTLP bytes.</param>
    /// <param name="contentType">The content type (application/x-protobuf or application/json).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExportAsync(
        ReadOnlyMemory<byte> metricData,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any pending metric data.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
