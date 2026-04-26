// <copyright file="ILogSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Abstractions;

/// <summary>
/// Defines the contract for a log sink that exports log data.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Exports log data (OTLP format).
    /// </summary>
    /// <param name="logData">The log data as OTLP bytes.</param>
    /// <param name="contentType">The content type (application/x-protobuf or application/json).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExportAsync(
        ReadOnlyMemory<byte> logData,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any pending log data.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
