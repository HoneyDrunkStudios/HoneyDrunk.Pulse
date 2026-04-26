// <copyright file="PulseCollectorOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// Configuration options for the Pulse Collector service.
/// </summary>
public sealed class PulseCollectorOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:Pulse:Collector";

    /// <summary>
    /// Gets or sets the service name for telemetry.
    /// </summary>
    public string ServiceName { get; set; } = "Pulse.Collector";

    /// <summary>
    /// Gets or sets the Studio identifier for Grid identity.
    /// </summary>
    public string StudioId { get; set; } = "honeydrunk";

    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// Gets or sets a value indicating whether Transport event publishing is enabled.
    /// </summary>
    public bool EnableTransportPublishing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the PostHog sink is enabled.
    /// Default is false; sinks must be explicitly enabled.
    /// </summary>
    public bool EnablePostHogSink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Sentry sink is enabled.
    /// Default is false; sinks must be explicitly enabled.
    /// </summary>
    public bool EnableSentrySink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Tempo sink is enabled.
    /// Default is false; sinks must be explicitly enabled.
    /// </summary>
    public bool EnableTempoSink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Loki sink is enabled.
    /// Default is false; sinks must be explicitly enabled.
    /// </summary>
    public bool EnableLokiSink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Mimir sink is enabled.
    /// Default is false; sinks must be explicitly enabled.
    /// </summary>
    public bool EnableMimirSink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Azure Monitor sink is enabled.
    /// Default is false; sinks must be explicitly enabled.
    /// </summary>
    public bool EnableAzureMonitorSink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to require authentication for OTLP endpoints.
    /// </summary>
    public bool RequireOtlpAuthentication { get; set; }

    /// <summary>
    /// Gets or sets the transport adapter to use.
    /// Valid values: "InMemory" (Development only), "AzureServiceBus".
    /// </summary>
    public string TransportAdapter { get; set; } = "InMemory";

    /// <summary>
    /// Gets or sets the Azure Service Bus queue or topic name.
    /// Required when TransportAdapter is "AzureServiceBus".
    /// </summary>
    public string? ServiceBusQueueOrTopicName { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size for processing.
    /// </summary>
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the processing timeout in seconds.
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; } = 30;
}
