// <copyright file="CollectorMetricDimensionsTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Pulse.Collector.Telemetry;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using System.Diagnostics.Metrics;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>Verifies the collector's bounded dimension vocabulary and tenant-format guard.</summary>
public sealed class CollectorMetricDimensionsTests
{
    /// <summary>Identifiers that are not external tenant ULIDs never become tenant labels.</summary>
    /// <param name="tenantId">The rejected tenant value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00000000000000000000000000")]
    [InlineData("user@example.test")]
    [InlineData("request-123")]
    [InlineData("invalid-tenant-id")]
    public void CollectorMetrics_RejectUnsafeTenantDimensions(string? tenantId)
    {
        var tags = Observe(tenantId);
        tags.Should().NotContain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.TenantId);
        tags.Should().OnlyContain(tag => tag.Key == "source.name" || tag.Key == "error.type");
    }

    /// <summary>Valid tenant values are allowed without adding user, request, or trace identifiers.</summary>
    [Fact]
    public void CollectorMetrics_EmitOnlyDeclaredDimensions()
    {
        const string tenantId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var tags = Observe(tenantId);
        tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.TenantId && Equals(tag.Value, tenantId));
        tags.Should().OnlyContain(tag => tag.Key == "source.name" || tag.Key == "error.type" || tag.Key == TelemetryTagKeys.HoneyDrunk.TenantId);
    }

    private static List<KeyValuePair<string, object?>> Observe(string? tenantId)
    {
        var observed = new List<KeyValuePair<string, object?>>();
        var sourceName = $"metric-test-{Guid.NewGuid()}";
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == CollectorTelemetry.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            if (tags.ToArray().Any(tag => tag.Key == "source.name" && Equals(tag.Value, sourceName)))
            {
                observed.AddRange(tags.ToArray());
            }
        });
        listener.Start();
        CollectorTelemetry.RecordTracesIngested(1, sourceName, tenantId);
        CollectorTelemetry.RecordLogsIngested(1, sourceName, tenantId);
        CollectorTelemetry.RecordMetricsIngested(1, sourceName, tenantId);
        CollectorTelemetry.RecordAnalyticsEventsIngested(1, sourceName, tenantId);
        observed.Should().NotBeEmpty();
        return observed;
    }
}
