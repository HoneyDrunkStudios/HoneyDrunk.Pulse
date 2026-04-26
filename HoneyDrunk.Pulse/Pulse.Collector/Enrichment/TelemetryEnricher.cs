// <copyright file="TelemetryEnricher.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Pulse.Collector.Configuration;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace HoneyDrunk.Pulse.Collector.Enrichment;

/// <summary>
/// Enriches telemetry data with HoneyDrunk context and required attributes.
/// </summary>
/// <remarks>
/// <para>
/// This enricher adds missing HoneyDrunk-specific tags and ensures required
/// OTLP resource attributes are present. It uses the Kernel's <see cref="IOperationContextAccessor"/>
/// to extract correlation information when available.
/// </para>
/// <para>
/// Enrichment rules:
/// <list type="bullet">
///   <item>If service.name is missing, defaults to "unknown-service"</item>
///   <item>Adds honeydrunk.node_id from Kernel context if available</item>
///   <item>Adds honeydrunk.correlation_id from operation context if available</item>
///   <item>Adds collector metadata (ingestion timestamp, environment)</item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="TelemetryEnricher"/> class.
/// </remarks>
/// <param name="operationContextAccessor">The operation context accessor (optional).</param>
/// <param name="options">The collector options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class TelemetryEnricher(
    IOperationContextAccessor? operationContextAccessor,
    IOptions<PulseCollectorOptions> options,
    ILogger<TelemetryEnricher> logger)
{
    private readonly PulseCollectorOptions _options = options.Value;

    /// <summary>
    /// Enriches resource attributes with HoneyDrunk context and defaults.
    /// </summary>
    /// <param name="existingAttributes">The existing resource attributes.</param>
    /// <returns>Enriched attributes dictionary.</returns>
    public Dictionary<string, object> EnrichResourceAttributes(
        IReadOnlyDictionary<string, object>? existingAttributes)
    {
        var enriched = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Copy existing attributes
        if (existingAttributes != null)
        {
            foreach (var kvp in existingAttributes)
            {
                enriched[kvp.Key] = kvp.Value;
            }
        }

        // Ensure service.name is present
        if (!enriched.ContainsKey(TelemetryTagKeys.Semantic.ServiceName))
        {
            enriched[TelemetryTagKeys.Semantic.ServiceName] = "unknown-service";
            LogDefaultServiceNameAdded();
        }

        // Add HoneyDrunk context from Kernel if available
        EnrichFromOperationContext(enriched);

        // Add collector metadata
        EnrichWithCollectorMetadata(enriched);

        return enriched;
    }

    /// <summary>
    /// Enriches span attributes with correlation context.
    /// </summary>
    /// <param name="existingAttributes">The existing span attributes.</param>
    /// <returns>Enriched attributes dictionary.</returns>
    public Dictionary<string, object> EnrichSpanAttributes(
        IReadOnlyDictionary<string, object>? existingAttributes)
    {
        var enriched = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Copy existing attributes
        if (existingAttributes != null)
        {
            foreach (var kvp in existingAttributes)
            {
                enriched[kvp.Key] = kvp.Value;
            }
        }

        // Add correlation context if not already present
        var operationContext = operationContextAccessor?.Current;
        if (operationContext != null)
        {
            TryAddAttribute(enriched, TelemetryTagKeys.HoneyDrunk.CorrelationId, operationContext.CorrelationId);
            TryAddAttribute(enriched, TelemetryTagKeys.HoneyDrunk.OperationId, operationContext.OperationId);
        }

        return enriched;
    }

    /// <summary>
    /// Enriches analytics event properties with context.
    /// </summary>
    /// <param name="existingProperties">The existing event properties.</param>
    /// <param name="serviceName">The source service name.</param>
    /// <returns>Enriched properties dictionary.</returns>
    public Dictionary<string, object?> EnrichAnalyticsEventProperties(
        IReadOnlyDictionary<string, object?>? existingProperties,
        string? serviceName)
    {
        var enriched = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Copy existing properties
        if (existingProperties != null)
        {
            foreach (var kvp in existingProperties)
            {
                enriched[kvp.Key] = kvp.Value;
            }
        }

        // Add source service if not present
        if (!enriched.ContainsKey(TelemetryTagKeys.Semantic.ServiceName) &&
            !string.IsNullOrEmpty(serviceName))
        {
            enriched[TelemetryTagKeys.Semantic.ServiceName] = serviceName;
        }

        // Add HoneyDrunk context
        var operationContext = operationContextAccessor?.Current;
        if (operationContext != null)
        {
            TryAddProperty(enriched, TelemetryTagKeys.HoneyDrunk.CorrelationId, operationContext.CorrelationId);
            TryAddProperty(enriched, TelemetryTagKeys.HoneyDrunk.OperationId, operationContext.OperationId);
        }

        // Add environment
        if (!string.IsNullOrEmpty(_options.Environment))
        {
            TryAddProperty(enriched, TelemetryTagKeys.HoneyDrunk.Environment, _options.Environment);
        }

        return enriched;
    }

    /// <summary>
    /// Enriches an error event with HoneyDrunk context.
    /// </summary>
    /// <param name="errorEvent">The error event to enrich.</param>
    /// <param name="sourceName">The source service name.</param>
    public void EnrichErrorEvent(ErrorEvent errorEvent, string? sourceName)
    {
        // Add source service tag if available
        if (!string.IsNullOrEmpty(sourceName) && !errorEvent.Tags.ContainsKey(TelemetryTagKeys.Semantic.ServiceName))
        {
            errorEvent.Tags[TelemetryTagKeys.Semantic.ServiceName] = sourceName;
        }

        // Add environment
        if (!string.IsNullOrEmpty(_options.Environment) && string.IsNullOrEmpty(errorEvent.Environment))
        {
            errorEvent.Environment = _options.Environment;
        }

        // Add HoneyDrunk context from operation context
        var operationContext = operationContextAccessor?.Current;
        if (operationContext != null)
        {
            if (string.IsNullOrEmpty(errorEvent.CorrelationId))
            {
                errorEvent.CorrelationId = operationContext.CorrelationId;
            }

            if (string.IsNullOrEmpty(errorEvent.OperationId))
            {
                errorEvent.OperationId = operationContext.OperationId;
            }

            if (operationContext.GridContext != null)
            {
                if (string.IsNullOrEmpty(errorEvent.NodeId))
                {
                    errorEvent.NodeId = operationContext.GridContext.NodeId;
                }

                if (!errorEvent.Tags.ContainsKey(TelemetryTagKeys.HoneyDrunk.TenantId) &&
                    !string.IsNullOrEmpty(operationContext.GridContext.TenantId))
                {
                    errorEvent.Tags[TelemetryTagKeys.HoneyDrunk.TenantId] = operationContext.GridContext.TenantId;
                }
            }
        }

        // Add ingestion timestamp
        if (!errorEvent.Extra.ContainsKey("pulse.ingested_at"))
        {
            errorEvent.Extra["pulse.ingested_at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// Enriches a TelemetryEvent (analytics) with HoneyDrunk context.
    /// </summary>
    /// <param name="telemetryEvent">The telemetry event to enrich.</param>
    /// <param name="sourceName">The source service name.</param>
    public void EnrichTelemetryEvent(TelemetryEvent telemetryEvent, string? sourceName)
    {
        // Add source service if not present
        if (!string.IsNullOrEmpty(sourceName) && string.IsNullOrEmpty(telemetryEvent.NodeName))
        {
            telemetryEvent.NodeName = sourceName;
        }

        // Add environment
        if (!string.IsNullOrEmpty(_options.Environment) && string.IsNullOrEmpty(telemetryEvent.Environment))
        {
            telemetryEvent.Environment = _options.Environment;
        }

        // Add HoneyDrunk context from operation context
        var operationContext = operationContextAccessor?.Current;
        if (operationContext != null)
        {
            if (string.IsNullOrEmpty(telemetryEvent.CorrelationId))
            {
                telemetryEvent.CorrelationId = operationContext.CorrelationId;
            }

            if (string.IsNullOrEmpty(telemetryEvent.OperationId))
            {
                telemetryEvent.OperationId = operationContext.OperationId;
            }

            if (operationContext.GridContext != null)
            {
                if (string.IsNullOrEmpty(telemetryEvent.NodeId))
                {
                    telemetryEvent.NodeId = operationContext.GridContext.NodeId;
                }

                if (string.IsNullOrEmpty(telemetryEvent.TenantId))
                {
                    telemetryEvent.TenantId = operationContext.GridContext.TenantId;
                }
            }
        }

        // Add ingestion timestamp to properties
        if (!telemetryEvent.Properties.ContainsKey("pulse.ingested_at"))
        {
            telemetryEvent.Properties["pulse.ingested_at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// Creates enrichment metadata dictionary for ingestion events.
    /// </summary>
    /// <param name="sourceName">The source service name.</param>
    /// <returns>Dictionary of enrichment metadata.</returns>
    public Dictionary<string, string> CreateIngestionMetadata(string? sourceName)
    {
        var metadata = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(sourceName))
        {
            metadata[TelemetryTagKeys.Semantic.ServiceName] = sourceName;
        }

        if (!string.IsNullOrEmpty(_options.Environment))
        {
            metadata[TelemetryTagKeys.HoneyDrunk.Environment] = _options.Environment;
        }

        var operationContext = operationContextAccessor?.Current;
        if (operationContext != null)
        {
            if (!string.IsNullOrEmpty(operationContext.CorrelationId))
            {
                metadata[TelemetryTagKeys.HoneyDrunk.CorrelationId] = operationContext.CorrelationId;
            }

            if (!string.IsNullOrEmpty(operationContext.OperationId))
            {
                metadata[TelemetryTagKeys.HoneyDrunk.OperationId] = operationContext.OperationId;
            }

            if (operationContext.GridContext != null)
            {
                if (!string.IsNullOrEmpty(operationContext.GridContext.NodeId))
                {
                    metadata[TelemetryTagKeys.HoneyDrunk.NodeId] = operationContext.GridContext.NodeId;
                }

                if (!string.IsNullOrEmpty(operationContext.GridContext.TenantId))
                {
                    metadata[TelemetryTagKeys.HoneyDrunk.TenantId] = operationContext.GridContext.TenantId;
                }
            }
        }

        metadata["pulse.ingested_at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

        return metadata;
    }

    /// <summary>
    /// Validates that required resource attributes are present.
    /// </summary>
    /// <param name="attributes">The resource attributes to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public bool ValidateResourceAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            LogResourceAttributesEmpty();
            return false;
        }

        // service.name is required per OTLP spec
        if (!attributes.ContainsKey(TelemetryTagKeys.Semantic.ServiceName))
        {
            LogMissingRequiredAttribute(TelemetryTagKeys.Semantic.ServiceName);
            return false;
        }

        return true;
    }

    private static void TryAddAttribute(
    Dictionary<string, object> attributes,
    string key,
    string? value)
    {
        if (!string.IsNullOrEmpty(value) && !attributes.ContainsKey(key))
        {
            attributes[key] = value;
        }
    }

    private static void TryAddProperty(
        Dictionary<string, object?> properties,
        string key,
        string? value)
    {
        if (!string.IsNullOrEmpty(value) && !properties.ContainsKey(key))
        {
            properties[key] = value;
        }
    }

    private void EnrichFromOperationContext(Dictionary<string, object> attributes)
    {
        var operationContext = operationContextAccessor?.Current;
        if (operationContext == null)
        {
            return;
        }

        TryAddAttribute(attributes, TelemetryTagKeys.HoneyDrunk.CorrelationId, operationContext.CorrelationId);
        TryAddAttribute(attributes, TelemetryTagKeys.HoneyDrunk.OperationId, operationContext.OperationId);

        // Add grid context if available
        if (operationContext.GridContext != null)
        {
            TryAddAttribute(attributes, TelemetryTagKeys.HoneyDrunk.NodeId, operationContext.GridContext.NodeId);
            TryAddAttribute(attributes, TelemetryTagKeys.HoneyDrunk.TenantId, operationContext.GridContext.TenantId);
            TryAddAttribute(attributes, TelemetryTagKeys.HoneyDrunk.Environment, operationContext.GridContext.Environment);
        }
    }

    private void EnrichWithCollectorMetadata(Dictionary<string, object> attributes)
    {
        // Add ingestion timestamp
        if (!attributes.ContainsKey("pulse.ingested_at"))
        {
            attributes["pulse.ingested_at"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // Add collector environment
        if (!string.IsNullOrEmpty(_options.Environment) &&
            !attributes.ContainsKey(TelemetryTagKeys.HoneyDrunk.Environment))
        {
            attributes[TelemetryTagKeys.HoneyDrunk.Environment] = _options.Environment;
        }
    }
}
