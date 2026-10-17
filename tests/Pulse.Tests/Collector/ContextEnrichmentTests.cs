using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Pulse.Collector.Configuration;
using HoneyDrunk.Pulse.Collector.Enrichment;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>Checks that enrichment fills missing context without replacing producer identity.</summary>
public sealed class ContextEnrichmentTests
{
    /// <summary>Errors, analytics, spans and resource attributes preserve producer context.</summary>
    /// <param name="preserve">Whether producer context is already populated.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ContextIsAddedOnlyWhenMissing(bool preserve)
    {
        var grid = Substitute.For<IGridContext>();
        grid.NodeId.Returns("node");
        var tenant = new TenantId(Ulid.NewUlid().ToString());
        grid.TenantId.Returns(tenant);
        grid.Environment.Returns("grid-env");
        var operation = Substitute.For<IOperationContext>();
        operation.CorrelationId.Returns("correlation");
        operation.OperationId.Returns("operation");
        operation.GridContext.Returns(grid);
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(operation);
        var enricher = new TelemetryEnricher(accessor, Options.Create(new PulseCollectorOptions { Environment = "collector-env" }), NullLogger<TelemetryEnricher>.Instance);
        var original = preserve ? "producer" : null;
        var error = new ErrorEvent { CorrelationId = original, OperationId = original, NodeId = original, Environment = original };
        var telemetry = new TelemetryEvent { EventName = "event", CorrelationId = original, OperationId = original, NodeId = original, TenantId = original, Environment = original, NodeName = original };
        if (preserve)
        {
            error.Tags[TelemetryTagKeys.HoneyDrunk.TenantId] = "producer";
            error.Tags[TelemetryTagKeys.Semantic.ServiceName] = "producer";
            error.Extra["pulse.ingested_at"] = 1L;
            telemetry.Properties["pulse.ingested_at"] = 1L;
        }

        enricher.EnrichErrorEvent(error, "service");
        enricher.EnrichTelemetryEvent(telemetry, "service");
        Assert.Equal(original ?? "correlation", error.CorrelationId);
        Assert.Equal(original ?? "operation", error.OperationId);
        Assert.Equal(original ?? "node", error.NodeId);
        Assert.Equal(original ?? tenant.ToString(), error.Tags[TelemetryTagKeys.HoneyDrunk.TenantId]);
        Assert.Equal(original ?? "service", error.Tags[TelemetryTagKeys.Semantic.ServiceName]);
        Assert.Equal(original ?? "collector-env", error.Environment);
        Assert.Equal(original ?? "correlation", telemetry.CorrelationId);
        Assert.Equal(original ?? "operation", telemetry.OperationId);
        Assert.Equal(original ?? "node", telemetry.NodeId);
        Assert.Equal(original ?? tenant.ToString(), telemetry.TenantId);
        Assert.Equal(original ?? "service", telemetry.NodeName);
        Assert.Equal(original ?? "collector-env", telemetry.Environment);
        if (preserve)
        {
            Assert.Equal(1L, error.Extra["pulse.ingested_at"]);
            Assert.Equal(1L, telemetry.Properties["pulse.ingested_at"]);
        }

        var attributes = new Dictionary<string, object>();
        var properties = new Dictionary<string, object?>();
        if (preserve)
        {
            attributes[TelemetryTagKeys.HoneyDrunk.CorrelationId] = "producer";
            properties[TelemetryTagKeys.HoneyDrunk.CorrelationId] = "producer";
        }

        Assert.Equal(original ?? "correlation", enricher.EnrichResourceAttributes(attributes)[TelemetryTagKeys.HoneyDrunk.CorrelationId]);
        Assert.Equal(original ?? "correlation", enricher.EnrichSpanAttributes(attributes)[TelemetryTagKeys.HoneyDrunk.CorrelationId]);
        Assert.Equal(original ?? "correlation", enricher.EnrichAnalyticsEventProperties(properties, "service")[TelemetryTagKeys.HoneyDrunk.CorrelationId]);
        var metadata = enricher.CreateIngestionMetadata("service");
        Assert.Equal("node", metadata[TelemetryTagKeys.HoneyDrunk.NodeId]);
        Assert.Equal(tenant.ToString(), metadata[TelemetryTagKeys.HoneyDrunk.TenantId]);
        Assert.Equal("correlation", metadata[TelemetryTagKeys.HoneyDrunk.CorrelationId]);
        Assert.Equal("operation", metadata[TelemetryTagKeys.HoneyDrunk.OperationId]);
    }
}
