// <copyright file="IErrorSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Models;

namespace HoneyDrunk.Telemetry.Abstractions.Abstractions;

/// <summary>
/// Defines the contract for an error tracking sink.
/// </summary>
public interface IErrorSink
{
    /// <summary>
    /// Captures an error event.
    /// </summary>
    /// <param name="errorEvent">The error event to capture.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CaptureAsync(ErrorEvent errorEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures an exception directly.
    /// </summary>
    /// <param name="exception">The exception to capture.</param>
    /// <param name="tags">Optional tags to associate with the error.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CaptureExceptionAsync(
        Exception exception,
        IDictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a message as an error event.
    /// </summary>
    /// <param name="message">The message to capture.</param>
    /// <param name="severity">The severity level.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CaptureMessageAsync(
        string message,
        TelemetryEventSeverity severity = TelemetryEventSeverity.Error,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any pending error events.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
