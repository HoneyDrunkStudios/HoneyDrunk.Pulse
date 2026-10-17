// <copyright file="PostHogBatchPayload.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace HoneyDrunk.Telemetry.Sink.PostHog.Mapping;

/// <summary>
/// Represents a PostHog batch payload.
/// </summary>
public sealed class PostHogBatchPayload
{
    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    [JsonPropertyName("api_key")]
    public required string ApiKey { get; set; }

    /// <summary>
    /// Gets the batch of events.
    /// </summary>
    [JsonPropertyName("batch")]
    public List<PostHogCaptureEvent> Batch { get; } = [];
}
