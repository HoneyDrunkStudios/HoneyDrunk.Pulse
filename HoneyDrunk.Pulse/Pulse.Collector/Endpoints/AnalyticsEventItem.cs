// <copyright file="AnalyticsEventItem.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Endpoints;

/// <summary>
/// Individual analytics event item.
/// </summary>
public sealed class AnalyticsEventItem
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    public required string EventName { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the distinct ID.
    /// </summary>
    public string? DistinctId { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the node ID.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Gets or sets the environment.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets additional properties.
    /// </summary>
    public Dictionary<string, object?>? Properties { get; init; }
}
