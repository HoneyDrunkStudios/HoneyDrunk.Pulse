// <copyright file="IAnalyticsEmitter.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Models;

namespace HoneyDrunk.Telemetry.Abstractions.Abstractions;

/// <summary>
/// Client-side abstraction for emitting analytics events to a telemetry collector.
/// </summary>
/// <remarks>
/// <para>
/// This interface should be used by application code to emit analytics events
/// without direct HTTP calls. Implementations handle communication with the
/// Pulse.Collector or other telemetry backends.
/// </para>
/// <para>
/// Unlike <see cref="IAnalyticsSink"/> which is used by the collector to
/// forward events to analytics backends (PostHog, etc.), this interface is
/// for applications that want to emit custom analytics events.
/// </para>
/// </remarks>
public interface IAnalyticsEmitter
{
    /// <summary>
    /// Emits an analytics event to the configured collector.
    /// </summary>
    /// <param name="telemetryEvent">The event to emit.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EmitAsync(TelemetryEvent telemetryEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Emits multiple analytics events to the configured collector.
    /// </summary>
    /// <param name="events">The events to emit.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EmitBatchAsync(IEnumerable<TelemetryEvent> events, CancellationToken cancellationToken = default);
}
