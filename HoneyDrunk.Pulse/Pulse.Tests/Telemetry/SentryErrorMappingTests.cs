// <copyright file="SentryErrorMappingTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.OpenTelemetry.Redaction;
using HoneyDrunk.Telemetry.Sink.Sentry.Implementation;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Tests native Sentry exception payloads without initializing a client or sending events.
/// </summary>
public class SentryErrorMappingTests
{
    /// <summary>
    /// Sanitized copies retain native exception fields without raw private exception data.
    /// </summary>
    [Fact]
    public void CreateExceptionEvent_ShouldPreserveSanitizedExceptionWithoutRawData()
    {
        var exception = new InvalidOperationException("password=outer-secret", new InvalidOperationException("private-inner-secret"));
        exception.Data["private-data"] = "private-data-secret";
        ExceptionDispatchInfo.SetRemoteStackTrace(exception, "   at Example.Service.Run() in /src/alice@example.com/Service.cs:line 12");
        var original = ErrorEvent.FromException(exception).WithTag("component", "checkout");
        var sanitized = TelemetryRedactor.RedactErrorEvent(original);

        var sentryEvent = SentrySink.CreateExceptionEvent(sanitized);

        sentryEvent.Should().NotBeNull();
        sentryEvent.Exception.Should().BeNull();
        sentryEvent.Level.Should().Be(SentryLevel.Error);
        var sentryException = sentryEvent.SentryExceptions.Should().ContainSingle().Which;
        sentryException.Type.Should().Be(typeof(InvalidOperationException).FullName);
        sentryException.Value.Should().Be("password=[REDACTED]");
        sentryException.Stacktrace.Should().BeNull();
        sentryEvent.Extra["exception.stacktrace"].Should().Be(sanitized.Extra["exception.stacktrace"]);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            sentryEvent.WriteTo(writer, null);
        }

        var payload = Encoding.UTF8.GetString(stream.ToArray());
        using var document = JsonDocument.Parse(payload);
        var nativeException = document.RootElement.GetProperty("exception").GetProperty("values")[0];
        nativeException.GetProperty("type").GetString().Should().Be(typeof(InvalidOperationException).FullName);
        nativeException.GetProperty("value").GetString().Should().Be("password=[REDACTED]");
        payload.Should().NotContain("outer-secret").And.NotContain("private-inner-secret")
            .And.NotContain("private-data-secret").And.NotContain("alice@example.com");
        original.Exception.Should().BeSameAs(exception);
        original.Message.Should().Be("password=outer-secret");
        exception.Data["private-data"].Should().Be("private-data-secret");
        original.Tags.Should().NotContainKey("exception.type");
        sanitized.Exception.Should().BeNull();
    }

    /// <summary>
    /// Both sanitized exception copies and extracted OTLP errors retain their native type.
    /// </summary>
    /// <param name="typeInTags">Whether the type is supplied in tags instead of extras.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateExceptionEvent_ShouldReadTypeFromTagsOrExtra(bool typeInTags)
    {
        var errorEvent = ErrorEvent.FromMessage("Operation failed");
        errorEvent.Severity = TelemetryEventSeverity.Fatal;
        if (typeInTags)
        {
            errorEvent.Tags["exception.type"] = "Example.OperationException";
        }
        else
        {
            errorEvent.Extra["exception.type"] = "Example.OperationException";
        }

        errorEvent.Extra["exception.message"] = "token=[REDACTED]";
        errorEvent.Extra["exception.stacktrace"] = "   at Example.Service.Run()";

        var sentryEvent = SentrySink.CreateExceptionEvent(errorEvent);

        sentryEvent.Should().NotBeNull();
        sentryEvent.Exception.Should().BeNull();
        sentryEvent.Level.Should().Be(SentryLevel.Fatal);
        var sentryException = sentryEvent.SentryExceptions.Should().ContainSingle().Which;
        sentryException.Type.Should().Be("Example.OperationException");
        sentryException.Value.Should().Be("token=[REDACTED]");
        sentryEvent.Extra["exception.stacktrace"].Should().Be("   at Example.Service.Run()");
        errorEvent.Extra["exception.message"].Should().Be("token=[REDACTED]");
    }

    /// <summary>
    /// Type-only exception metadata falls back to the event message without fabricating frames.
    /// </summary>
    [Fact]
    public void CreateExceptionEvent_ShouldUseEventMessageWhenExceptionMessageIsMissing()
    {
        var errorEvent = ErrorEvent.FromMessage("Operation failed")
            .WithTag("exception.type", "Example.OperationException");

        var sentryEvent = SentrySink.CreateExceptionEvent(errorEvent);

        sentryEvent.Should().NotBeNull();
        var sentryException = sentryEvent.SentryExceptions.Should().ContainSingle().Which;
        sentryException.Value.Should().Be("Operation failed");
        sentryException.Stacktrace.Should().BeNull();
        sentryEvent.Extra.Should().BeEmpty();
    }

    /// <summary>
    /// Ordinary message events keep the existing CaptureMessage path.
    /// </summary>
    [Fact]
    public void CreateExceptionEvent_ShouldNotConvertMessageOnlyEvents()
    {
        var errorEvent = ErrorEvent.FromMessage("Operation failed");

        var sentryEvent = SentrySink.CreateExceptionEvent(errorEvent);

        sentryEvent.Should().BeNull();
    }

    /// <summary>
    /// Raw direct-sink exceptions keep the existing CaptureException path.
    /// </summary>
    [Fact]
    public void CreateExceptionEvent_ShouldNotReplaceRawDirectSinkExceptions()
    {
        var exception = new InvalidOperationException("Original exception");
        var errorEvent = ErrorEvent.FromException(exception)
            .WithTag("exception.type", "Example.OtherException");

        var sentryEvent = SentrySink.CreateExceptionEvent(errorEvent);

        sentryEvent.Should().BeNull();
        errorEvent.Exception.Should().BeSameAs(exception);
    }

    /// <summary>
    /// Non-string metadata cannot be serialized or converted through arbitrary ToString calls.
    /// </summary>
    [Fact]
    public void CreateExceptionEvent_ShouldIgnoreNonStringExceptionMetadata()
    {
        var errorEvent = ErrorEvent.FromMessage("Operation failed");
        errorEvent.Extra["exception.type"] = new { Secret = "private-secret" };
        errorEvent.Extra["exception.message"] = 123;
        errorEvent.Extra["exception.stacktrace"] = new object();

        var sentryEvent = SentrySink.CreateExceptionEvent(errorEvent);

        sentryEvent.Should().BeNull();
    }

    /// <summary>
    /// Sanitized .NET frames remain usable for native grouping after both collector passes.
    /// </summary>
    [Fact]
    public void CreateExceptionEvent_ShouldRetainNativeFramesAfterRepeatedRedaction()
    {
        var exception = Assert.Throws<InvalidOperationException>(ThrowExceptionForMapping);
        var original = ErrorEvent.FromException(exception);
        var sanitized = TelemetryRedactor.RedactErrorEvent(TelemetryRedactor.RedactErrorEvent(original));

        var sentryEvent = SentrySink.CreateExceptionEvent(sanitized);

        sentryEvent.Should().NotBeNull();
        sentryEvent.Exception.Should().BeNull();
        var sentryException = sentryEvent.SentryExceptions.Should().ContainSingle().Which;
        sentryException.Stacktrace.Should().NotBeNull();
        sentryException.Stacktrace.Frames.Should().NotBeEmpty();
        sentryException.Stacktrace.Frames.Last().Function.Should().EndWith(".ThrowExceptionForMapping");
        original.Extra.Should().NotContainKey("exception.stacktrace.frames");
    }

    /// <summary>
    /// Structured metadata uses only allowed scalar fields and retains oldest-first order.
    /// </summary>
    [Fact]
    public void CreateExceptionEvent_ShouldMapStructuredFramesWithoutRetainingObjects()
    {
        var errorEvent = ErrorEvent.FromMessage("Operation failed")
            .WithTag("exception.type", "Example.OperationException");
        errorEvent.Extra["exception.stacktrace.frames"] = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["function"] = "Example.Controller.Handle",
                ["filename"] = "/src/Controller.cs",
                ["lineno"] = 30,
                ["colno"] = 5,
                ["unexpected"] = new InvalidOperationException("private-data"),
            },
            new()
            {
                ["function"] = "Example.Service.Run",
                ["lineno"] = -1,
                ["colno"] = "42",
            },
        };

        var sentryEvent = SentrySink.CreateExceptionEvent(errorEvent);

        sentryEvent.Should().NotBeNull();
        var sentryException = sentryEvent.SentryExceptions.Should().ContainSingle().Which;
        sentryException.Stacktrace.Should().NotBeNull();
        var frames = sentryException.Stacktrace.Frames;
        frames.Should().HaveCount(2);
        frames[0].Function.Should().Be("Example.Controller.Handle");
        frames[0].FileName.Should().Be("/src/Controller.cs");
        frames[0].LineNumber.Should().Be(30);
        frames[0].ColumnNumber.Should().Be(5);
        frames[1].Function.Should().Be("Example.Service.Run");
        frames[1].LineNumber.Should().BeNull();
        frames[1].ColumnNumber.Should().BeNull();
        sentryEvent.Extra.Should().BeEmpty();
    }

    private static void ThrowExceptionForMapping()
        => throw new InvalidOperationException("password=private-secret");
}
