// <copyright file="HoneyDrunkOpenTelemetryOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.OpenTelemetry.Options;

/// <summary>
/// Configuration options for HoneyDrunk OpenTelemetry instrumentation.
/// </summary>
public sealed class HoneyDrunkOpenTelemetryOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:OpenTelemetry";

    /// <summary>
    /// Gets or sets the service name for telemetry identification.
    /// </summary>
    public string ServiceName { get; set; } = "UnknownService";

    /// <summary>
    /// Gets or sets the service version.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the service namespace.
    /// </summary>
    public string ServiceNamespace { get; set; } = "HoneyDrunk";

    /// <summary>
    /// Gets or sets the service instance ID.
    /// </summary>
    public string? ServiceInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the environment name (e.g., Production, Staging, Development).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the OTLP exporter endpoint URL.
    /// </summary>
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the OTLP exporter protocol (grpc or http/protobuf).
    /// </summary>
    public OtlpExportProtocol ExportProtocol { get; set; } = OtlpExportProtocol.Grpc;

    /// <summary>
    /// Gets or sets a value indicating whether tracing is enabled.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether metrics are enabled.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether logging export is enabled.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether ASP.NET Core instrumentation is enabled.
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether HTTP client instrumentation is enabled.
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether runtime instrumentation is enabled.
    /// </summary>
    public bool EnableRuntimeInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether process instrumentation is enabled.
    /// </summary>
    public bool EnableProcessInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets additional activity sources to listen to.
    /// </summary>
    public List<string> AdditionalActivitySources { get; } = [];

    /// <summary>
    /// Gets additional meters to collect from.
    /// </summary>
    public List<string> AdditionalMeters { get; } = [];
}
