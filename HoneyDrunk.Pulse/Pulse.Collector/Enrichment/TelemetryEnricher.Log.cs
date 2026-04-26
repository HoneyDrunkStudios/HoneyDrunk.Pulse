// <copyright file="TelemetryEnricher.Log.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Enrichment;

/// <summary>
/// LoggerMessage source-generated logging methods for TelemetryEnricher.
/// </summary>
public sealed partial class TelemetryEnricher
{
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Debug,
        Message = "Added default service.name to telemetry")]
    private partial void LogDefaultServiceNameAdded();

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Warning,
        Message = "Resource attributes are empty")]
    private partial void LogResourceAttributesEmpty();

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Warning,
        Message = "Missing required attribute: {AttributeName}")]
    private partial void LogMissingRequiredAttribute(string attributeName);
}
