// <copyright file="PostHogEventMapper.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using HoneyDrunk.Telemetry.Sink.PostHog.Options;

namespace HoneyDrunk.Telemetry.Sink.PostHog.Mapping;

/// <summary>
/// Maps <see cref="TelemetryEvent"/> to PostHog capture event payloads.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PostHogEventMapper"/> class.
/// </remarks>
/// <param name="options">The PostHog sink options.</param>
public sealed class PostHogEventMapper(PostHogSinkOptions options)
{
    private readonly HashSet<string> _excludedKeys = new(options.ExcludedPropertyKeys, StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string>? _approvedKeys = options.ApprovedPropertyKeys.Count > 0
            ? new HashSet<string>(options.ApprovedPropertyKeys, StringComparer.OrdinalIgnoreCase)
            : null;

    /// <summary>
    /// Maps a telemetry event to a PostHog capture event payload.
    /// </summary>
    /// <param name="telemetryEvent">The telemetry event.</param>
    /// <returns>The PostHog capture event.</returns>
    public PostHogCaptureEvent Map(TelemetryEvent telemetryEvent)
    {
        var distinctId = ResolveDistinctId(telemetryEvent);
        var properties = BuildProperties(telemetryEvent);

        return new PostHogCaptureEvent
        {
            Event = telemetryEvent.EventName,
            DistinctId = distinctId,
            Timestamp = telemetryEvent.Timestamp,
            Properties = properties,
        };
    }

    /// <summary>
    /// Maps multiple telemetry events to a PostHog batch payload.
    /// </summary>
    /// <param name="events">The telemetry events.</param>
    /// <param name="apiKey">The current PostHog API key from Vault.</param>
    /// <returns>The PostHog batch payload.</returns>
    public PostHogBatchPayload MapBatch(IEnumerable<TelemetryEvent> events, string apiKey)
    {
        var payload = new PostHogBatchPayload { ApiKey = apiKey };
        foreach (var evt in events.Select(Map))
        {
            payload.Batch.Add(evt);
        }

        return payload;
    }

    private static string ResolveDistinctId(TelemetryEvent telemetryEvent)
    {
        // Priority: DistinctId > UserId > SessionId > Anonymous
        if (!string.IsNullOrEmpty(telemetryEvent.DistinctId))
        {
            return telemetryEvent.DistinctId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.UserId))
        {
            return telemetryEvent.UserId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.SessionId))
        {
            return $"session:{telemetryEvent.SessionId}";
        }

        // Generate anonymous ID based on correlation context
        if (!string.IsNullOrEmpty(telemetryEvent.CorrelationId))
        {
            return $"anon:{telemetryEvent.CorrelationId}";
        }

        return $"anon:{Guid.NewGuid():N}";
    }

    private static void AddIfNotEmpty(Dictionary<string, object?> dict, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            dict[key] = value;
        }
    }

    private Dictionary<string, object?> BuildProperties(TelemetryEvent telemetryEvent)
    {
        var properties = new Dictionary<string, object?>();

        // Add HoneyDrunk correlation context
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.CorrelationId, telemetryEvent.CorrelationId);
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.OperationId, telemetryEvent.OperationId);
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.NodeId, telemetryEvent.NodeId);
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.NodeName, telemetryEvent.NodeName);
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.GridId, telemetryEvent.GridId);
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.TenantId, telemetryEvent.TenantId);
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.SessionId, telemetryEvent.SessionId);
        AddIfNotEmpty(properties, TelemetryTagKeys.HoneyDrunk.Environment, telemetryEvent.Environment);

        // Add custom properties (with filtering)
        foreach (var property in telemetryEvent.Properties)
        {
            if (ShouldIncludeProperty(property.Key))
            {
                properties[property.Key] = property.Value;
            }
        }

        return properties;
    }

    private bool ShouldIncludeProperty(string key)
    {
        // Always exclude keys in the exclusion list
        if (_excludedKeys.Contains(key))
        {
            return false;
        }

        // If we have an approved list, only include approved keys
        if (_approvedKeys is not null)
        {
            return _approvedKeys.Contains(key);
        }

        return true;
    }
}
