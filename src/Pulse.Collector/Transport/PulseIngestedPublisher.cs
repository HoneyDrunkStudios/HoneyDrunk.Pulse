// <copyright file="PulseIngestedPublisher.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Transport.Abstractions;

namespace HoneyDrunk.Pulse.Collector.Transport;

/// <summary>
/// Publishes PulseIngested events via HoneyDrunk.Transport.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PulseIngestedPublisher"/> class.
/// </remarks>
/// <param name="messagePublisher">The transport message publisher.</param>
/// <param name="gridContextAccessor">The grid context accessor.</param>
/// <param name="nodeContext">The node context for Grid identity.</param>
/// <param name="logger">The logger.</param>
public sealed partial class PulseIngestedPublisher(
    IMessagePublisher messagePublisher,
    IGridContextAccessor gridContextAccessor,
    INodeContext nodeContext,
    ILogger<PulseIngestedPublisher> logger)
{
    /// <summary>
    /// The destination topic for PulseIngested events.
    /// </summary>
    public const string DestinationTopic = "pulse-ingested";

    /// <summary>
    /// Publishes a PulseIngested event.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishAsync(Contracts.Events.PulseIngested @event, CancellationToken cancellationToken = default)
    {
        var gridContext = gridContextAccessor.GridContext is { IsInitialized: true }
            ? gridContextAccessor.GridContext
            : CreateDefaultGridContext(@event.CorrelationId);

        LogPublishingPulseIngested(
            @event.BatchId ?? "unknown",
            @event.TraceCount,
            @event.MetricCount,
            @event.LogCount,
            @event.AnalyticsEventCount);

        await messagePublisher.PublishAsync(
            DestinationTopic,
            @event,
            gridContext,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a fallback <see cref="GridContext"/> when the ambient one is missing or uninitialized.
    /// Prefers the event's own <see cref="Contracts.Events.PulseIngested.CorrelationId"/> so that the
    /// Transport message context stays correlated with the ingested event; only mints a new GUID
    /// when the event itself has no correlation ID set.
    /// </summary>
    private GridContext CreateDefaultGridContext(string? eventCorrelationId)
    {
        var context = new GridContext(
            nodeId: nodeContext.NodeId,
            studioId: nodeContext.StudioId,
            environment: nodeContext.Environment);

        context.Initialize(
            correlationId: !string.IsNullOrWhiteSpace(eventCorrelationId)
                ? eventCorrelationId
                : Guid.NewGuid().ToString("N"),
            causationId: null,
            tenantId: null,
            projectId: null);

        return context;
    }
}
