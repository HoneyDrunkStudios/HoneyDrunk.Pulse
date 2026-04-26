// <copyright file="SentrySinkTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Sink.Sentry.Options;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Tests for Sentry sink configuration.
/// </summary>
public class SentrySinkTests
{
    /// <summary>
    /// Verifies that validation fails when enabled without a DSN secret name.
    /// </summary>
    [Fact]
    public void SentrySinkOptions_ShouldValidate_WhenEnabledWithoutDsnSecretName()
    {
        // Arrange
        var options = new SentrySinkOptions
        {
            Enabled = true,
            DsnSecretName = string.Empty,
        };

        // Act
        var action = options.Validate;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*DSN*required*");
    }

    /// <summary>
    /// Verifies that validation succeeds when disabled without a DSN secret name.
    /// </summary>
    [Fact]
    public void SentrySinkOptions_ShouldNotThrow_WhenDisabledWithoutDsnSecretName()
    {
        // Arrange
        var options = new SentrySinkOptions
        {
            Enabled = false,
            DsnSecretName = string.Empty,
        };

        // Act
        var action = options.Validate;

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that validation succeeds when enabled with a DSN secret name.
    /// </summary>
    [Fact]
    public void SentrySinkOptions_ShouldNotThrow_WhenEnabledWithDsnSecretName()
    {
        // Arrange
        var options = new SentrySinkOptions
        {
            Enabled = true,
            DsnSecretName = "Sentry--Dsn",
        };

        // Act
        var action = options.Validate;

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that an error event from exception captures the message.
    /// </summary>
    [Fact]
    public void ErrorEvent_FromException_ShouldCaptureMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error message");

        // Act
        var errorEvent = ErrorEvent.FromException(exception);

        // Assert
        errorEvent.Exception.Should().Be(exception);
        errorEvent.Message.Should().Be("Test error message");
    }

    /// <summary>
    /// Verifies that an error event from message sets the message.
    /// </summary>
    [Fact]
    public void ErrorEvent_FromMessage_ShouldSetMessage()
    {
        // Act
        var errorEvent = ErrorEvent.FromMessage("Something went wrong");

        // Assert
        errorEvent.Message.Should().Be("Something went wrong");
        errorEvent.Exception.Should().BeNull();
    }

    /// <summary>
    /// Verifies that tags are added to the error event.
    /// </summary>
    [Fact]
    public void ErrorEvent_WithTag_ShouldAddTag()
    {
        // Act
        var errorEvent = ErrorEvent.FromMessage("Error")
            .WithTag("component", "auth")
            .WithTag("severity", "high");

        // Assert
        errorEvent.Tags.Should().ContainKey("component");
        errorEvent.Tags["component"].Should().Be("auth");
        errorEvent.Tags.Should().ContainKey("severity");
        errorEvent.Tags["severity"].Should().Be("high");
    }

    /// <summary>
    /// Verifies that correlation ID is set on the error event.
    /// </summary>
    [Fact]
    public void ErrorEvent_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Act
        var errorEvent = ErrorEvent.FromMessage("Error")
            .WithCorrelationId("corr-12345");

        // Assert
        errorEvent.CorrelationId.Should().Be("corr-12345");
    }

    /// <summary>
    /// Verifies that the default severity is Error.
    /// </summary>
    [Fact]
    public void ErrorEvent_DefaultSeverity_ShouldBeError()
    {
        // Act
        var errorEvent = ErrorEvent.FromMessage("Error");

        // Assert
        errorEvent.Severity.Should().Be(TelemetryEventSeverity.Error);
    }
}
