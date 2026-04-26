// <copyright file="TelemetryEnricherTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Pulse.Collector.Configuration;
using HoneyDrunk.Pulse.Collector.Enrichment;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Tests for the TelemetryEnricher class.
/// </summary>
public class TelemetryEnricherTests
{
    private readonly TelemetryEnricher _enricher;
    private readonly PulseCollectorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryEnricherTests"/> class.
    /// </summary>
    public TelemetryEnricherTests()
    {
        _options = new PulseCollectorOptions
        {
            Environment = "test-environment",
            ServiceName = "test-collector",
        };

        _enricher = new TelemetryEnricher(
            operationContextAccessor: null,
            Options.Create(_options),
            NullLogger<TelemetryEnricher>.Instance);
    }

    /// <summary>
    /// Tests that empty attributes get a default service name.
    /// </summary>
    [Fact]
    public void EnrichResourceAttributes_WithEmptyAttributes_AddsServiceNameDefault()
    {
        // Arrange
        Dictionary<string, object>? emptyAttributes = null;

        // Act
        var enriched = _enricher.EnrichResourceAttributes(emptyAttributes);

        // Assert
        enriched.Should().ContainKey(TelemetryTagKeys.Semantic.ServiceName);
        enriched[TelemetryTagKeys.Semantic.ServiceName].Should().Be("unknown-service");
    }

    /// <summary>
    /// Tests that existing service name is not overwritten.
    /// </summary>
    [Fact]
    public void EnrichResourceAttributes_WithExistingServiceName_DoesNotOverwrite()
    {
        // Arrange
        var existing = new Dictionary<string, object>
        {
            [TelemetryTagKeys.Semantic.ServiceName] = "my-service",
        };

        // Act
        var enriched = _enricher.EnrichResourceAttributes(existing);

        // Assert
        enriched[TelemetryTagKeys.Semantic.ServiceName].Should().Be("my-service");
    }

    /// <summary>
    /// Tests that ingestion timestamp is added to attributes.
    /// </summary>
    [Fact]
    public void EnrichResourceAttributes_AddsIngestionTimestamp()
    {
        // Arrange
        var beforeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var enriched = _enricher.EnrichResourceAttributes(null);
        var afterTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Assert
        enriched.Should().ContainKey("pulse.ingested_at");
        var ingestedAt = (long)enriched["pulse.ingested_at"];
        ingestedAt.Should().BeInRange(beforeTime, afterTime);
    }

    /// <summary>
    /// Tests that environment is added to resource attributes.
    /// </summary>
    [Fact]
    public void EnrichResourceAttributes_AddsEnvironment()
    {
        // Arrange & Act
        var enriched = _enricher.EnrichResourceAttributes(null);

        // Assert
        enriched.Should().ContainKey(TelemetryTagKeys.HoneyDrunk.Environment);
        enriched[TelemetryTagKeys.HoneyDrunk.Environment].Should().Be("test-environment");
    }

    /// <summary>
    /// Tests that service name is added to analytics event properties.
    /// </summary>
    [Fact]
    public void EnrichAnalyticsEventProperties_AddsServiceName_WhenNotPresent()
    {
        // Arrange
        var properties = new Dictionary<string, object?>();

        // Act
        var enriched = _enricher.EnrichAnalyticsEventProperties(properties, "source-service");

        // Assert
        enriched.Should().ContainKey(TelemetryTagKeys.Semantic.ServiceName);
        enriched[TelemetryTagKeys.Semantic.ServiceName].Should().Be("source-service");
    }

    /// <summary>
    /// Tests that existing properties are preserved during enrichment.
    /// </summary>
    [Fact]
    public void EnrichAnalyticsEventProperties_PreservesExistingProperties()
    {
        // Arrange
        var properties = new Dictionary<string, object?>
        {
            ["custom_key"] = "custom_value",
            ["another_key"] = 42,
        };

        // Act
        var enriched = _enricher.EnrichAnalyticsEventProperties(properties, "service");

        // Assert
        enriched["custom_key"].Should().Be("custom_value");
        enriched["another_key"].Should().Be(42);
    }

    /// <summary>
    /// Tests that null attributes fail validation.
    /// </summary>
    [Fact]
    public void ValidateResourceAttributes_ReturnsFalse_WhenAttributesAreNull()
    {
        // Act
        var result = _enricher.ValidateResourceAttributes(null);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that missing service name fails validation.
    /// </summary>
    [Fact]
    public void ValidateResourceAttributes_ReturnsFalse_WhenServiceNameMissing()
    {
        // Arrange
        var attributes = new Dictionary<string, object>
        {
            ["some.attribute"] = "value",
        };

        // Act
        var result = _enricher.ValidateResourceAttributes(attributes);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that validation passes when service name is present.
    /// </summary>
    [Fact]
    public void ValidateResourceAttributes_ReturnsTrue_WhenServiceNamePresent()
    {
        // Arrange
        var attributes = new Dictionary<string, object>
        {
            [TelemetryTagKeys.Semantic.ServiceName] = "my-service",
        };

        // Act
        var result = _enricher.ValidateResourceAttributes(attributes);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that existing span attributes are preserved during enrichment.
    /// </summary>
    [Fact]
    public void EnrichSpanAttributes_PreservesExistingAttributes()
    {
        // Arrange
        var attributes = new Dictionary<string, object>
        {
            ["http.method"] = "GET",
            ["http.url"] = "/api/test",
        };

        // Act
        var enriched = _enricher.EnrichSpanAttributes(attributes);

        // Assert
        enriched["http.method"].Should().Be("GET");
        enriched["http.url"].Should().Be("/api/test");
    }

    /// <summary>
    /// Tests that environment is added to analytics event properties.
    /// </summary>
    [Fact]
    public void EnrichAnalyticsEventProperties_AddsEnvironment()
    {
        // Arrange
        var properties = new Dictionary<string, object?>();

        // Act
        var enriched = _enricher.EnrichAnalyticsEventProperties(properties, "service");

        // Assert
        enriched.Should().ContainKey(TelemetryTagKeys.HoneyDrunk.Environment);
        enriched[TelemetryTagKeys.HoneyDrunk.Environment].Should().Be("test-environment");
    }
}
