// <copyright file="TelemetryEvent.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Models;

/// <summary>
/// Represents an analytics event that can be routed to analytics sinks like PostHog.
/// </summary>
public sealed class TelemetryEvent
{
    /// <summary>
    /// Gets or sets the name of the event.
    /// </summary>
    public required string EventName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the distinct identifier for the user or entity.
    /// This is used by analytics platforms to track unique users.
    /// </summary>
    public string? DistinctId { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with this event.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the session ID for tracking user sessions.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the operation ID.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the node ID where the event originated.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Gets or sets the node name where the event originated.
    /// </summary>
    public string? NodeName { get; set; }

    /// <summary>
    /// Gets or sets the grid ID this event belongs to.
    /// </summary>
    public string? GridId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets additional properties associated with this event.
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = [];

    /// <summary>
    /// Creates a new telemetry event with the specified name.
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <returns>A new <see cref="TelemetryEvent"/> instance.</returns>
    public static TelemetryEvent Create(string eventName)
        => new() { EventName = eventName };

    /// <summary>
    /// Adds a property to the event.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public TelemetryEvent WithProperty(string key, object? value)
    {
        Properties[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the distinct ID for this event.
    /// </summary>
    /// <param name="distinctId">The distinct ID.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public TelemetryEvent WithDistinctId(string distinctId)
    {
        DistinctId = distinctId;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID for this event.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public TelemetryEvent WithCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }
}
