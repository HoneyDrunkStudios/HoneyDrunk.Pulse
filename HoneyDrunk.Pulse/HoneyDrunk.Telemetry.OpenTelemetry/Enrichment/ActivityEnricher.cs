// <copyright file="ActivityEnricher.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using System.Diagnostics;

namespace HoneyDrunk.Telemetry.OpenTelemetry.Enrichment;

/// <summary>
/// Enriches activities with HoneyDrunk context tags.
/// </summary>
public static class ActivityEnricher
{
    /// <summary>
    /// Enriches an activity with HoneyDrunk operation context.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="context">The operation context.</param>
    public static void EnrichWithOperationContext(Activity? activity, IOperationContext? context)
    {
        if (activity is null || context is null)
        {
            return;
        }

        activity.SetTag(TelemetryTagKeys.HoneyDrunk.CorrelationId, context.CorrelationId);
        activity.SetTag(TelemetryTagKeys.HoneyDrunk.OperationId, context.OperationId);

        if (!string.IsNullOrEmpty(context.TenantId))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.TenantId, context.TenantId);
        }
    }

    /// <summary>
    /// Enriches an activity with HoneyDrunk grid context.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="context">The grid context.</param>
    public static void EnrichWithGridContext(Activity? activity, IGridContext? context)
    {
        if (activity is null || context is null)
        {
            return;
        }

        activity.SetTag(TelemetryTagKeys.HoneyDrunk.CorrelationId, context.CorrelationId);
        activity.SetTag(TelemetryTagKeys.HoneyDrunk.NodeId, context.NodeId);
        activity.SetTag(TelemetryTagKeys.HoneyDrunk.Environment, context.Environment);

        if (!string.IsNullOrEmpty(context.TenantId))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.TenantId, context.TenantId);
        }
    }

    /// <summary>
    /// Enriches an activity with node identity information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="nodeId">The node ID.</param>
    /// <param name="nodeName">The node name.</param>
    /// <param name="nodeType">The node type.</param>
    public static void EnrichWithNodeIdentity(Activity? activity, string? nodeId, string? nodeName, string? nodeType)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(nodeId))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.NodeId, nodeId);
        }

        if (!string.IsNullOrEmpty(nodeName))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.NodeName, nodeName);
        }

        if (!string.IsNullOrEmpty(nodeType))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.NodeType, nodeType);
        }
    }

    /// <summary>
    /// Enriches an activity with tenant and grid information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="gridId">The grid ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    public static void EnrichWithGridContext(Activity? activity, string? gridId, string? tenantId)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(gridId))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.GridId, gridId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.TenantId, tenantId);
        }
    }

    /// <summary>
    /// Enriches an activity with user context.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="sessionId">The session ID.</param>
    public static void EnrichWithUserContext(Activity? activity, string? userId, string? sessionId)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(userId))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.UserId, userId);
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.SessionId, sessionId);
        }
    }

    /// <summary>
    /// Enriches an activity with environment information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="environment">The environment name.</param>
    /// <param name="version">The service version.</param>
    public static void EnrichWithEnvironment(Activity? activity, string? environment, string? version)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(environment))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.Environment, environment);
        }

        if (!string.IsNullOrEmpty(version))
        {
            activity.SetTag(TelemetryTagKeys.HoneyDrunk.Version, version);
        }
    }
}
