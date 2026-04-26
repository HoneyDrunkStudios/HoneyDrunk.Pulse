// <copyright file="ITraceSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Abstractions;

/// <summary>
/// Defines the contract for a trace sink that exports trace data.
/// </summary>
public interface ITraceSink
{
    /// <summary>
    /// Exports trace data (OTLP format).
    /// </summary>
    /// <param name="traceData">The trace data as OTLP bytes.</param>
    /// <param name="contentType">The content type (application/x-protobuf or application/json).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExportAsync(
        ReadOnlyMemory<byte> traceData,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any pending trace data.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
