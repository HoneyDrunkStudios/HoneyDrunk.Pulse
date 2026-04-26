// <copyright file="PulseIngested.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Contracts.Events;

/// <summary>
/// Transport event published when telemetry has been ingested by Pulse.Collector.
/// </summary>
public sealed class PulseIngested
{
    /// <summary>
    /// Gets or sets the contract version for forward compatibility.
    /// </summary>
    public int Version { get; set; } = PulseContractVersions.Current;

    /// <summary>
    /// Gets or sets the timestamp when the ingestion occurred.
    /// </summary>
    public DateTimeOffset IngestionTimestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the unique identifier for this ingestion batch.
    /// </summary>
    public string? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the source node or service name that sent the telemetry.
    /// </summary>
    public string? SourceNodeName { get; set; }

    /// <summary>
    /// Gets or sets the source node ID.
    /// </summary>
    public string? SourceNodeId { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for this ingestion batch.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the count of traces ingested.
    /// </summary>
    public int TraceCount { get; set; }

    /// <summary>
    /// Gets or sets the count of metrics ingested.
    /// </summary>
    public int MetricCount { get; set; }

    /// <summary>
    /// Gets or sets the count of logs ingested.
    /// </summary>
    public int LogCount { get; set; }

    /// <summary>
    /// Gets or sets the count of analytics events ingested.
    /// </summary>
    public int AnalyticsEventCount { get; set; }

    /// <summary>
    /// Gets or sets the ingestion status.
    /// </summary>
    public IngestionStatus Status { get; set; } = IngestionStatus.Success;

    /// <summary>
    /// Gets or sets the error message if ingestion partially or fully failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the processing duration in milliseconds.
    /// </summary>
    public long ProcessingDurationMs { get; set; }

    /// <summary>
    /// Gets additional metadata about the ingestion.
    /// </summary>
    public Dictionary<string, string> Metadata { get; } = [];
}
