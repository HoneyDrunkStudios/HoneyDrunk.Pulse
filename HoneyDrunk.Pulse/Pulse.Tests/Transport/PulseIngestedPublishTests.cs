// <copyright file="PulseIngestedPublishTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Pulse.Contracts;
using HoneyDrunk.Pulse.Contracts.Events;

namespace HoneyDrunk.Pulse.Tests.Transport;

/// <summary>
/// Tests for PulseIngested event contract.
/// </summary>
public class PulseIngestedPublishTests
{
    /// <summary>
    /// Verifies that PulseIngested has the correct version.
    /// </summary>
    [Fact]
    public void PulseIngested_ShouldHaveCorrectVersion()
    {
        // Act
        var ingested = new PulseIngested();

        // Assert
        ingested.Version.Should().Be(PulseContractVersions.Current);
    }

    /// <summary>
    /// Verifies that PulseIngested initializes with default values.
    /// </summary>
    [Fact]
    public void PulseIngested_ShouldInitializeWithDefaults()
    {
        // Act — bracket the construction so we can assert the timestamp falls within the window
        // rather than relying on a tight tolerance against UtcNow (flaky on slow CI / GC pauses).
        var before = DateTimeOffset.UtcNow;
        var ingested = new PulseIngested();
        var after = DateTimeOffset.UtcNow;

        // Assert
        ingested.Status.Should().Be(IngestionStatus.Success);
        ingested.IngestionTimestamp.Should().BeOnOrAfter(before);
        ingested.IngestionTimestamp.Should().BeOnOrBefore(after);
        ingested.TraceCount.Should().Be(0);
        ingested.MetricCount.Should().Be(0);
        ingested.LogCount.Should().Be(0);
        ingested.AnalyticsEventCount.Should().Be(0);
        ingested.Metadata.Should().NotBeNull();
        ingested.Metadata.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that all properties can be set on PulseIngested.
    /// </summary>
    [Fact]
    public void PulseIngested_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var ingested = new PulseIngested
        {
            BatchId = "batch-123",
            SourceNodeName = "TestService",
            SourceNodeId = "node-456",
            CorrelationId = "corr-789",
            TraceCount = 10,
            MetricCount = 20,
            LogCount = 30,
            AnalyticsEventCount = 5,
            Status = IngestionStatus.PartialSuccess,
            ErrorMessage = "Some items failed",
            ProcessingDurationMs = 150,
        };
        ingested.Metadata["key1"] = "value1";

        // Assert
        ingested.BatchId.Should().Be("batch-123");
        ingested.SourceNodeName.Should().Be("TestService");
        ingested.SourceNodeId.Should().Be("node-456");
        ingested.CorrelationId.Should().Be("corr-789");
        ingested.TraceCount.Should().Be(10);
        ingested.MetricCount.Should().Be(20);
        ingested.LogCount.Should().Be(30);
        ingested.AnalyticsEventCount.Should().Be(5);
        ingested.Status.Should().Be(IngestionStatus.PartialSuccess);
        ingested.ErrorMessage.Should().Be("Some items failed");
        ingested.ProcessingDurationMs.Should().Be(150);
        ingested.Metadata.Should().ContainKey("key1");
    }

    /// <summary>
    /// Verifies that IngestionStatus enum values work correctly.
    /// </summary>
    /// <param name="status">The status value to test.</param>
    [Theory]
    [InlineData(IngestionStatus.Success)]
    [InlineData(IngestionStatus.PartialSuccess)]
    [InlineData(IngestionStatus.Failed)]
    [InlineData(IngestionStatus.Skipped)]
    public void IngestionStatus_ShouldHaveExpectedValues(IngestionStatus status)
    {
        // Act
        var ingested = new PulseIngested { Status = status };

        // Assert
        ingested.Status.Should().Be(status);
    }
}
