// <copyright file="PostHogMappingTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using HoneyDrunk.Telemetry.Sink.PostHog.Mapping;
using HoneyDrunk.Telemetry.Sink.PostHog.Options;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Tests for PostHog event mapping.
/// </summary>
public class PostHogMappingTests
{
    /// <summary>
    /// Verifies that the event name is correctly mapped.
    /// </summary>
    [Fact]
    public void Map_ShouldMapEventName()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = TelemetryEvent.Create("user.signup");

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.Event.Should().Be("user.signup");
    }

    /// <summary>
    /// Verifies that the distinct ID is used when provided.
    /// </summary>
    [Fact]
    public void Map_ShouldUseDistinctIdWhenProvided()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithDistinctId("user-123");

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.DistinctId.Should().Be("user-123");
    }

    /// <summary>
    /// Verifies that user ID is used as fallback when distinct ID is not provided.
    /// </summary>
    [Fact]
    public void Map_ShouldFallbackToUserIdWhenDistinctIdNotProvided()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = new TelemetryEvent
        {
            EventName = "test.event",
            UserId = "user-456",
        };

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.DistinctId.Should().Be("user-456");
    }

    /// <summary>
    /// Verifies that session ID with prefix is used when no user info is available.
    /// </summary>
    [Fact]
    public void Map_ShouldUseSessionIdWithPrefixWhenNoUserInfo()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = new TelemetryEvent
        {
            EventName = "test.event",
            SessionId = "session-789",
        };

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.DistinctId.Should().Be("session:session-789");
    }

    /// <summary>
    /// Verifies that correlation tags are included in the mapped event.
    /// </summary>
    [Fact]
    public void Map_ShouldIncludeCorrelationTags()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = new TelemetryEvent
        {
            EventName = "test.event",
            DistinctId = "user-1",
            CorrelationId = "corr-123",
            OperationId = "op-456",
            NodeId = "node-789",
        };

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.Properties.Should().ContainKey(TelemetryTagKeys.HoneyDrunk.CorrelationId);
        result.Properties[TelemetryTagKeys.HoneyDrunk.CorrelationId].Should().Be("corr-123");
        result.Properties.Should().ContainKey(TelemetryTagKeys.HoneyDrunk.OperationId);
        result.Properties[TelemetryTagKeys.HoneyDrunk.OperationId].Should().Be("op-456");
        result.Properties.Should().ContainKey(TelemetryTagKeys.HoneyDrunk.NodeId);
        result.Properties[TelemetryTagKeys.HoneyDrunk.NodeId].Should().Be("node-789");
    }

    /// <summary>
    /// Verifies that tenant context is included in the mapped event.
    /// </summary>
    [Fact]
    public void Map_ShouldIncludeTenantId()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = new TelemetryEvent
        {
            EventName = "test.event",
            DistinctId = "user-1",
            TenantId = "tenant-acme",
        };

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.Properties.Should().ContainKey(TelemetryTagKeys.HoneyDrunk.TenantId);
        result.Properties[TelemetryTagKeys.HoneyDrunk.TenantId].Should().Be("tenant-acme");
    }

    /// <summary>
    /// Verifies that custom properties are included in the mapped event.
    /// </summary>
    [Fact]
    public void Map_ShouldIncludeCustomProperties()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithDistinctId("user-1")
            .WithProperty("plan", "premium")
            .WithProperty("feature_count", 5);

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.Properties.Should().ContainKey("plan");
        result.Properties["plan"].Should().Be("premium");
        result.Properties.Should().ContainKey("feature_count");
        result.Properties["feature_count"].Should().Be(5);
    }

    /// <summary>
    /// Verifies that keys in the exclusion list are excluded from the mapped event.
    /// </summary>
    [Fact]
    public void Map_ShouldExcludeKeysInExclusionList()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        options.ExcludedPropertyKeys.AddRange(["secret", "password"]);
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithDistinctId("user-1")
            .WithProperty("secret", "should-not-appear")
            .WithProperty("password", "also-hidden")
            .WithProperty("visible", "should-appear");

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.Properties.Should().NotContainKey("secret");
        result.Properties.Should().NotContainKey("password");
        result.Properties.Should().ContainKey("visible");
    }

    /// <summary>
    /// Verifies that only approved keys are included when an approved list is provided.
    /// </summary>
    [Fact]
    public void Map_ShouldOnlyIncludeApprovedKeysWhenListProvided()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        options.ApprovedPropertyKeys.Add("allowed_key");
        var mapper = new PostHogEventMapper(options);
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithDistinctId("user-1")
            .WithProperty("allowed_key", "included")
            .WithProperty("not_allowed", "excluded");

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.Properties.Should().ContainKey("allowed_key");
        result.Properties.Should().NotContainKey("not_allowed");
    }

    /// <summary>
    /// Verifies that the timestamp is preserved in the mapped event.
    /// </summary>
    [Fact]
    public void Map_ShouldPreserveTimestamp()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var timestamp = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var telemetryEvent = new TelemetryEvent
        {
            EventName = "test.event",
            DistinctId = "user-1",
            Timestamp = timestamp,
        };

        // Act
        var result = mapper.Map(telemetryEvent);

        // Assert
        result.Timestamp.Should().Be(timestamp);
    }

    /// <summary>
    /// Verifies that batch mapping creates a payload with the API key.
    /// </summary>
    [Fact]
    public void MapBatch_ShouldCreateBatchPayloadWithApiKey()
    {
        // Arrange
        var options = new PostHogSinkOptions();
        var mapper = new PostHogEventMapper(options);
        var events = new[]
        {
            TelemetryEvent.Create("event1").WithDistinctId("user1"),
            TelemetryEvent.Create("event2").WithDistinctId("user2"),
        };

        // Act
        var result = mapper.MapBatch(events, "test-api-key");

        // Assert
        result.ApiKey.Should().Be("test-api-key");
        result.Batch.Should().HaveCount(2);
        result.Batch[0].Event.Should().Be("event1");
        result.Batch[1].Event.Should().Be("event2");
    }
}
