// <copyright file="ErrorEvent.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Models;

/// <summary>
/// Represents an error event that can be routed to error tracking sinks like Sentry.
/// </summary>
public sealed class ErrorEvent
{
    /// <summary>
    /// Gets or sets the exception associated with this error.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the error.
    /// </summary>
    public TelemetryEventSeverity Severity { get; set; } = TelemetryEventSeverity.Error;

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the operation ID.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the node ID where the error originated.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with this error.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the service/release version.
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// Gets additional tags associated with this error.
    /// </summary>
    public Dictionary<string, string> Tags { get; } = [];

    /// <summary>
    /// Gets additional contextual data.
    /// </summary>
    public Dictionary<string, object?> Extra { get; } = [];

    /// <summary>
    /// Creates a new error event from an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A new <see cref="ErrorEvent"/> instance.</returns>
    public static ErrorEvent FromException(Exception exception)
        => new()
        {
            Exception = exception,
            Message = exception.Message,
        };

    /// <summary>
    /// Creates a new error event with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new <see cref="ErrorEvent"/> instance.</returns>
    public static ErrorEvent FromMessage(string message)
        => new() { Message = message };

    /// <summary>
    /// Adds a tag to the error event.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public ErrorEvent WithTag(string key, string value)
    {
        Tags[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public ErrorEvent WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }
}
