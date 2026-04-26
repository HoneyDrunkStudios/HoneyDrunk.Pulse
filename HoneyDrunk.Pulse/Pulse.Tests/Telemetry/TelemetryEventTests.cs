// <copyright file="TelemetryEventTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Telemetry.Abstractions.Models;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Tests for the TelemetryEvent model.
/// </summary>
public class TelemetryEventTests
{
    /// <summary>
    /// Tests that Create factory method sets event name.
    /// </summary>
    [Fact]
    public void Create_SetsEventName()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("user.signup");

        // Assert
        telemetryEvent.EventName.Should().Be("user.signup");
    }

    /// <summary>
    /// Tests that Create factory method sets timestamp.
    /// </summary>
    [Fact]
    public void Create_SetsTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event");

        // Assert
        var after = DateTimeOffset.UtcNow;
        telemetryEvent.Timestamp.Should().BeOnOrAfter(before);
        telemetryEvent.Timestamp.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Tests that WithDistinctId fluently sets distinct ID.
    /// </summary>
    [Fact]
    public void WithDistinctId_SetsDistinctId()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithDistinctId("user-123");

        // Assert
        telemetryEvent.DistinctId.Should().Be("user-123");
    }

    /// <summary>
    /// Tests that WithProperty adds a property.
    /// </summary>
    [Fact]
    public void WithProperty_AddsProperty()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithProperty("plan", "premium");

        // Assert
        telemetryEvent.Properties.Should().ContainKey("plan");
        telemetryEvent.Properties["plan"].Should().Be("premium");
    }

    /// <summary>
    /// Tests that multiple properties can be added.
    /// </summary>
    [Fact]
    public void WithProperty_MultipleProperties_AddsAll()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithProperty("plan", "premium")
            .WithProperty("feature_count", 5)
            .WithProperty("is_trial", false);

        // Assert
        telemetryEvent.Properties.Should().HaveCount(3);
        telemetryEvent.Properties["plan"].Should().Be("premium");
        telemetryEvent.Properties["feature_count"].Should().Be(5);
        telemetryEvent.Properties["is_trial"].Should().Be(false);
    }

    /// <summary>
    /// Tests that WithCorrelationId sets the correlation ID.
    /// </summary>
    [Fact]
    public void WithCorrelationId_SetsCorrelationId()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithCorrelationId("corr-12345");

        // Assert
        telemetryEvent.CorrelationId.Should().Be("corr-12345");
    }

    /// <summary>
    /// Tests that chaining all methods works correctly.
    /// </summary>
    [Fact]
    public void FluentChaining_SetsAllProperties()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("user.upgraded")
            .WithDistinctId("user-123")
            .WithCorrelationId("corr-abc")
            .WithProperty("new_plan", "enterprise")
            .WithProperty("seats", 10);

        // Assert
        telemetryEvent.EventName.Should().Be("user.upgraded");
        telemetryEvent.DistinctId.Should().Be("user-123");
        telemetryEvent.CorrelationId.Should().Be("corr-abc");
        telemetryEvent.Properties.Should().ContainKey("new_plan");
        telemetryEvent.Properties.Should().ContainKey("seats");
    }

    /// <summary>
    /// Tests that default properties collection is empty.
    /// </summary>
    [Fact]
    public void Create_PropertiesAreEmpty()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event");

        // Assert
        telemetryEvent.Properties.Should().NotBeNull();
        telemetryEvent.Properties.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that WithProperty overwrites existing property.
    /// </summary>
    [Fact]
    public void WithProperty_ExistingKey_Overwrites()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithProperty("key", "value1")
            .WithProperty("key", "value2");

        // Assert
        telemetryEvent.Properties["key"].Should().Be("value2");
    }

    /// <summary>
    /// Tests that null values are handled for properties.
    /// </summary>
    [Fact]
    public void WithProperty_NullValue_IsAccepted()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event")
            .WithProperty("nullable_field", null);

        // Assert
        telemetryEvent.Properties.Should().ContainKey("nullable_field");
        telemetryEvent.Properties["nullable_field"].Should().BeNull();
    }

    /// <summary>
    /// Tests that UserId can be set and retrieved.
    /// </summary>
    [Fact]
    public void UserId_CanBeSetAndRetrieved()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event");
        telemetryEvent.UserId = "user-456";

        // Assert
        telemetryEvent.UserId.Should().Be("user-456");
    }

    /// <summary>
    /// Tests that SessionId can be set and retrieved.
    /// </summary>
    [Fact]
    public void SessionId_CanBeSetAndRetrieved()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event");
        telemetryEvent.SessionId = "session-789";

        // Assert
        telemetryEvent.SessionId.Should().Be("session-789");
    }

    /// <summary>
    /// Tests that OperationId can be set and retrieved.
    /// </summary>
    [Fact]
    public void OperationId_CanBeSetAndRetrieved()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event");
        telemetryEvent.OperationId = "op-123";

        // Assert
        telemetryEvent.OperationId.Should().Be("op-123");
    }

    /// <summary>
    /// Tests that NodeId can be set and retrieved.
    /// </summary>
    [Fact]
    public void NodeId_CanBeSetAndRetrieved()
    {
        // Act
        var telemetryEvent = TelemetryEvent.Create("test.event");
        telemetryEvent.NodeId = "node-abc";

        // Assert
        telemetryEvent.NodeId.Should().Be("node-abc");
    }
}
