// <copyright file="TelemetryTagKeys.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Tags;

/// <summary>
/// Defines standard tag/property keys used across OpenTelemetry and analytics sinks.
/// </summary>
public static class TelemetryTagKeys
{
    /// <summary>
    /// The prefix used for all HoneyDrunk-specific tags.
    /// </summary>
    public const string Prefix = "honeydrunk";

    /// <summary>
    /// HoneyDrunk identity and context tags.
    /// </summary>
    public static class HoneyDrunk
    {
        /// <summary>
        /// The unique identifier for a node in the Grid.
        /// </summary>
        public const string NodeId = $"{Prefix}.node_id";

        /// <summary>
        /// The name of the node.
        /// </summary>
        public const string NodeName = $"{Prefix}.node_name";

        /// <summary>
        /// The type of the node.
        /// </summary>
        public const string NodeType = $"{Prefix}.node_type";

        /// <summary>
        /// The correlation ID for tracking requests across services.
        /// </summary>
        public const string CorrelationId = $"{Prefix}.correlation_id";

        /// <summary>
        /// The operation ID for a specific operation.
        /// </summary>
        public const string OperationId = $"{Prefix}.operation_id";

        /// <summary>
        /// The Grid ID this node belongs to.
        /// </summary>
        public const string GridId = $"{Prefix}.grid_id";

        /// <summary>
        /// The tenant ID for multi-tenant scenarios.
        /// </summary>
        /// <remarks>
        /// ADR-0026 treats this as a low-cardinality telemetry dimension: v1 paying customers are
        /// expected in the tens, and continued use as a metric tag is bounded by Notify Cloud's
        /// cardinality kill criteria. Do not substitute user, session, node, or request identifiers here.
        /// </remarks>
        public const string TenantId = $"{Prefix}.tenant_id";

        /// <summary>
        /// The user ID associated with the operation.
        /// </summary>
        public const string UserId = $"{Prefix}.user_id";

        /// <summary>
        /// The session ID for user sessions.
        /// </summary>
        public const string SessionId = $"{Prefix}.session_id";

        /// <summary>
        /// The environment name (e.g., production, staging).
        /// </summary>
        public const string Environment = $"{Prefix}.environment";

        /// <summary>
        /// The version of the service/node.
        /// </summary>
        public const string Version = $"{Prefix}.version";
    }

    /// <summary>
    /// Standard OpenTelemetry semantic convention keys.
    /// </summary>
    public static class Semantic
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public const string ServiceName = "service.name";

        /// <summary>
        /// The version of the service.
        /// </summary>
        public const string ServiceVersion = "service.version";

        /// <summary>
        /// The namespace of the service.
        /// </summary>
        public const string ServiceNamespace = "service.namespace";

        /// <summary>
        /// The instance ID of the service.
        /// </summary>
        public const string ServiceInstanceId = "service.instance.id";

        /// <summary>
        /// The deployment environment.
        /// </summary>
        public const string DeploymentEnvironment = "deployment.environment";

        /// <summary>
        /// The host name.
        /// </summary>
        public const string HostName = "host.name";
    }

    /// <summary>
    /// Analytics-specific keys for product events.
    /// </summary>
    public static class Analytics
    {
        /// <summary>
        /// The distinct ID for analytics (typically user ID).
        /// </summary>
        public const string DistinctId = "distinct_id";

        /// <summary>
        /// The event name.
        /// </summary>
        public const string EventName = "event";

        /// <summary>
        /// The timestamp of the event.
        /// </summary>
        public const string Timestamp = "timestamp";

        /// <summary>
        /// Properties associated with the event.
        /// </summary>
        public const string Properties = "properties";

        /// <summary>
        /// The feature name for feature usage tracking.
        /// </summary>
        public const string FeatureName = $"{Prefix}.feature_name";

        /// <summary>
        /// The action performed.
        /// </summary>
        public const string Action = $"{Prefix}.action";

        /// <summary>
        /// The result of the action.
        /// </summary>
        public const string Result = $"{Prefix}.result";

        /// <summary>
        /// Duration of the action in milliseconds.
        /// </summary>
        public const string DurationMs = $"{Prefix}.duration_ms";
    }

    /// <summary>
    /// Error and exception related keys.
    /// </summary>
    public static class Error
    {
        /// <summary>
        /// The error type.
        /// </summary>
        public const string Type = "error.type";

        /// <summary>
        /// The error message.
        /// </summary>
        public const string Message = "error.message";

        /// <summary>
        /// The error stack trace.
        /// </summary>
        public const string StackTrace = "error.stack_trace";

        /// <summary>
        /// Whether this is a handled exception.
        /// </summary>
        public const string Handled = "error.handled";
    }
}
