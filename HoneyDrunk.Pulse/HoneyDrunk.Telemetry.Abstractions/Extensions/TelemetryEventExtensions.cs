// <copyright file="TelemetryEventExtensions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;

namespace HoneyDrunk.Telemetry.Abstractions.Extensions;

/// <summary>
/// Extension methods for <see cref="TelemetryEvent"/>.
/// </summary>
public static class TelemetryEventExtensions
{
    /// <summary>
    /// Enriches a telemetry event with HoneyDrunk operation context.
    /// </summary>
    /// <param name="telemetryEvent">The telemetry event to enrich.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>The enriched telemetry event for fluent chaining.</returns>
    public static TelemetryEvent WithOperationContext(this TelemetryEvent telemetryEvent, IOperationContext? context)
    {
        if (context is null)
        {
            return telemetryEvent;
        }

        telemetryEvent.CorrelationId ??= context.CorrelationId;
        telemetryEvent.OperationId ??= context.OperationId;
        telemetryEvent.TenantId ??= context.TenantId;

        return telemetryEvent;
    }

    /// <summary>
    /// Enriches a telemetry event with HoneyDrunk grid context.
    /// </summary>
    /// <param name="telemetryEvent">The telemetry event to enrich.</param>
    /// <param name="context">The grid context.</param>
    /// <returns>The enriched telemetry event for fluent chaining.</returns>
    public static TelemetryEvent WithGridContext(this TelemetryEvent telemetryEvent, IGridContext? context)
    {
        if (context is null)
        {
            return telemetryEvent;
        }

        telemetryEvent.CorrelationId ??= context.CorrelationId;
        telemetryEvent.NodeId ??= context.NodeId;
        telemetryEvent.TenantId ??= context.TenantId;
        telemetryEvent.Environment ??= context.Environment;

        return telemetryEvent;
    }

    /// <summary>
    /// Enriches a telemetry event with HoneyDrunk node context.
    /// </summary>
    /// <param name="telemetryEvent">The telemetry event to enrich.</param>
    /// <param name="context">The node context.</param>
    /// <returns>The enriched telemetry event for fluent chaining.</returns>
    public static TelemetryEvent WithNodeContext(this TelemetryEvent telemetryEvent, INodeContext? context)
    {
        if (context is null)
        {
            return telemetryEvent;
        }

        telemetryEvent.NodeId ??= context.NodeId;
        telemetryEvent.Environment ??= context.Environment;

        return telemetryEvent;
    }

    /// <summary>
    /// Converts the telemetry event properties to a tag dictionary suitable for tracing.
    /// </summary>
    /// <param name="telemetryEvent">The telemetry event.</param>
    /// <returns>A dictionary of tags.</returns>
    public static Dictionary<string, object?> ToTagDictionary(this TelemetryEvent telemetryEvent)
    {
        var tags = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(telemetryEvent.NodeId))
        {
            tags[TelemetryTagKeys.HoneyDrunk.NodeId] = telemetryEvent.NodeId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.NodeName))
        {
            tags[TelemetryTagKeys.HoneyDrunk.NodeName] = telemetryEvent.NodeName;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.CorrelationId))
        {
            tags[TelemetryTagKeys.HoneyDrunk.CorrelationId] = telemetryEvent.CorrelationId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.OperationId))
        {
            tags[TelemetryTagKeys.HoneyDrunk.OperationId] = telemetryEvent.OperationId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.GridId))
        {
            tags[TelemetryTagKeys.HoneyDrunk.GridId] = telemetryEvent.GridId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.TenantId))
        {
            tags[TelemetryTagKeys.HoneyDrunk.TenantId] = telemetryEvent.TenantId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.UserId))
        {
            tags[TelemetryTagKeys.HoneyDrunk.UserId] = telemetryEvent.UserId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.SessionId))
        {
            tags[TelemetryTagKeys.HoneyDrunk.SessionId] = telemetryEvent.SessionId;
        }

        if (!string.IsNullOrEmpty(telemetryEvent.Environment))
        {
            tags[TelemetryTagKeys.HoneyDrunk.Environment] = telemetryEvent.Environment;
        }

        // Merge custom properties
        foreach (var property in telemetryEvent.Properties)
        {
            tags[property.Key] = property.Value;
        }

        return tags;
    }
}
