// <copyright file="IAnalyticsSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Models;

namespace HoneyDrunk.Telemetry.Abstractions.Abstractions;

/// <summary>
/// Defines the contract for an analytics sink that can capture telemetry events.
/// </summary>
public interface IAnalyticsSink
{
    /// <summary>
    /// Captures an analytics event.
    /// </summary>
    /// <param name="telemetryEvent">The event to capture.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CaptureAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures multiple analytics events in batch.
    /// </summary>
    /// <param name="events">The events to capture.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CaptureBatchAsync(IEnumerable<TelemetryEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any pending events.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
