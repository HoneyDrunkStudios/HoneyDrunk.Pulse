// <copyright file="PostHogCaptureEvent.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace HoneyDrunk.Telemetry.Sink.PostHog.Mapping;

/// <summary>
/// Represents a single PostHog capture event.
/// </summary>
public sealed class PostHogCaptureEvent
{
    /// <summary>
    /// Gets or sets the event name.
    /// </summary>
    [JsonPropertyName("event")]
    public required string Event { get; set; }

    /// <summary>
    /// Gets or sets the distinct ID.
    /// </summary>
    [JsonPropertyName("distinct_id")]
    public required string DistinctId { get; set; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets the event properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object?> Properties { get; init; } = [];
}
