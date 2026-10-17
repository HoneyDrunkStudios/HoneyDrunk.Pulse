// <copyright file="AnalyticsEventsRequest.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Endpoints;

/// <summary>
/// Request model for analytics events.
/// </summary>
public sealed class AnalyticsEventsRequest
{
    /// <summary>
    /// Gets the list of events.
    /// </summary>
    public List<AnalyticsEventItem>? Events { get; init; }

    /// <summary>
    /// Gets or sets the source service name.
    /// </summary>
    public string? SourceService { get; set; }

    /// <summary>
    /// Gets or sets the source node ID.
    /// </summary>
    public string? SourceNodeId { get; set; }
}
