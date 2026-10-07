// <copyright file="TelemetryRedactionProcessorTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Telemetry.OpenTelemetry.Extensions;
using HoneyDrunk.Telemetry.OpenTelemetry.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text.Json;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Exercises real OpenTelemetry pipelines to verify the data received by exporters.
/// </summary>
public sealed class TelemetryRedactionProcessorTests
{
    /// <summary>
    /// Exported spans have sanitized duplicate tags, events, links, names, and status text.
    /// </summary>
    [Fact]
    public void TraceProcessor_RedactsBeforeExportWithoutChangingTraceIdentity()
    {
        Activity? exported = null;
        var sourceName = $"redaction-test-{Guid.NewGuid()}";
        using var source = new ActivitySource(sourceName);
        using var exporter = new CaptureExporter<Activity>(value => exported = value);
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddProcessor(_ => new TelemetryRedactionProcessor())
            .AddProcessor(_ => new SimpleActivityExportProcessor(exporter))
            .Build();
        var parent = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        var linkContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        using var activity = source.StartActivity("token=span-sensitive", ActivityKind.Internal, parent);
        activity.Should().NotBeNull();
        if (activity is null)
        {
            return;
        }

        activity.AddTag("password", "first-sensitive");
        activity.AddTag("password", "second-sensitive");
        activity.SetTag("honeydrunk.correlation_id", "business-42");
        activity.SetStatus(ActivityStatusCode.Error, "Contact somebody@example.test");
        activity.AddEvent(new ActivityEvent("email=event@example.test", tags: new ActivityTagsCollection
        {
            ["exception.message"] = "password=event-sensitive",
            ["exception.stacktrace"] = "at handler email=stack@example.test",
        }));
        activity.AddLink(new ActivityLink(linkContext, new ActivityTagsCollection { ["api_key"] = "link-sensitive" }));
        var spanId = activity.SpanId;

        activity.Stop();

        exported.Should().BeSameAs(activity);
        activity.TraceId.Should().Be(parent.TraceId);
        activity.SpanId.Should().Be(spanId);
        activity.ParentSpanId.Should().Be(parent.SpanId);
        activity.GetTagItem("honeydrunk.correlation_id").Should().Be("business-42");
        activity.TagObjects.Where(tag => tag.Key == "password").Should().OnlyContain(tag => Equals(tag.Value, TelemetryRedactor.RedactedValue));
        activity.DisplayName.Should().NotContain("span-sensitive");
        activity.StatusDescription.Should().NotContain("somebody@example.test");
        activity.Events.Single().Name.Should().NotContain("event@example.test");
        JsonSerializer.Serialize(activity.Events.Single().Tags).Should().NotContain("event-sensitive").And.NotContain("stack@example.test");
        activity.Links.Single().Context.Should().Be(linkContext);
        activity.Links.Single().Tags.Should().Contain(tag => tag.Key == "api_key" && Equals(tag.Value, TelemetryRedactor.RedactedValue));
    }

    /// <summary>
    /// Structured secret values cannot leak through preformatted messages or exception objects.
    /// </summary>
    [Fact]
    public void LogProcessor_RedactsAttributesBodyFormattedMessageAndException()
    {
        string? exportedText = null;
        Exception? exportedException = null;
        ActivityTraceId traceId = default;
        ActivitySpanId spanId = default;
        var exportedCount = 0;
        using var exporter = new CaptureExporter<LogRecord>(record =>
        {
            exportedCount++;
            exportedText = JsonSerializer.Serialize(new { record.Body, record.FormattedMessage, record.Attributes });
            exportedException = record.Exception;
            traceId = record.TraceId;
            spanId = record.SpanId;
        });
        using var factory = LoggerFactory.Create(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = false;
            options.AddProcessor(new LogRedactionProcessor());
            options.AddProcessor(new SimpleLogRecordExportProcessor(exporter));
        }));
        using var activity = new Activity("log-test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        var exception = new InvalidOperationException("password=exception-sensitive");
        exception.Data["secret"] = "exception-data";
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("Password", "structured-sensitive"),
            new("honeydrunk.correlation_id", "business-42"),
            new("{OriginalFormat}", "Value {Password}"),
        };

        factory.CreateLogger("redaction-test").Log(LogLevel.Error, default, attributes, exception, (_, _) => "Value structured-sensitive");

        exportedCount.Should().Be(1);
        exportedText.Should().NotContain("structured-sensitive").And.NotContain("exception-sensitive").And.NotContain("exception-data");
        exportedText.Should().Contain("business-42").And.Contain("exception.type").And.Contain(TelemetryRedactor.RedactedValue);
        exportedException.Should().BeNull();
        traceId.Should().Be(activity.TraceId);
        spanId.Should().Be(activity.SpanId);
    }

    /// <summary>
    /// Copying safe collections does not erase useful formatted log messages.
    /// </summary>
    [Fact]
    public void LogProcessor_SafeCollections_PreservesFormattedMessage()
    {
        string? formattedMessage = null;
        using var exporter = new CaptureExporter<LogRecord>(record => formattedMessage = record.FormattedMessage);
        using var factory = LoggerFactory.Create(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(new LogRedactionProcessor());
            options.AddProcessor(new SimpleLogRecordExportProcessor(exporter));
        }));
        using var json = JsonDocument.Parse("{\"count\":2,\"labels\":[\"east\",\"west\"]}");
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("Numbers", new long[] { 1, 2 }),
            new("Labels", new[] { "east", "west" }),
            new("Payload", new Dictionary<string, object?> { ["plan"] = "standard" }),
            new("Json", json.RootElement),
            new("{OriginalFormat}", "Numbers {Numbers}; labels {Labels}; payload {Payload}"),
        };
        const string expected = "Numbers 1, 2; labels east, west; payload plan standard";

        factory.CreateLogger("redaction-test").Log(LogLevel.Information, default, attributes, null, (_, _) => expected);

        formattedMessage.Should().Be(expected);
    }

    /// <summary>
    /// Changes inside arrays and nested dictionaries still suppress their unlabeled formatted copies.
    /// </summary>
    /// <param name="nestedDictionary">Whether to nest the sensitive field inside an array.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LogProcessor_SensitiveCollections_SuppressesFormattedMessage(bool nestedDictionary)
    {
        string? formattedMessage = null;
        string? exportedAttributes = null;
        using var exporter = new CaptureExporter<LogRecord>(record =>
        {
            formattedMessage = record.FormattedMessage;
            exportedAttributes = JsonSerializer.Serialize(record.Attributes);
        });
        using var factory = LoggerFactory.Create(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(new LogRedactionProcessor());
            options.AddProcessor(new SimpleLogRecordExportProcessor(exporter));
        }));
        object collection = nestedDictionary
            ? new object?[] { new Dictionary<string, object?> { ["password"] = "array-sensitive" } }
            : new[] { "benign", "token=array-sensitive" };
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("Payload", collection),
            new("{OriginalFormat}", "Value {Payload}"),
        };

        factory.CreateLogger("redaction-test").Log(LogLevel.Information, default, attributes, null, (_, _) => "Value array-sensitive");

        formattedMessage.Should().Be(TelemetryRedactor.RedactedValue);
        exportedAttributes.Should().NotContain("array-sensitive");
    }

    /// <summary>
    /// Free-text logs are sanitized even when no structured state is available.
    /// </summary>
    [Fact]
    public void LogProcessor_RedactsUnstructuredBody()
    {
        string? exportedText = null;
        using var exporter = new CaptureExporter<LogRecord>(record => exportedText = $"{record.Body} {record.FormattedMessage}");
        using var factory = LoggerFactory.Create(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(new LogRedactionProcessor());
            options.AddProcessor(new SimpleLogRecordExportProcessor(exporter));
        }));

        factory.CreateLogger("redaction-test").Log(LogLevel.Warning, default, "email=somebody@example.test password=hidden", null, (state, _) => state);

        exportedText.Should().NotContain("somebody@example.test").And.NotContain("hidden");
    }

    /// <summary>
    /// Default registration does not collect immutable scopes that bypass attribute processors.
    /// </summary>
    [Fact]
    public void DefaultRegistration_DisablesUnredactableScopes()
    {
        var services = new ServiceCollection();
        services.AddHoneyDrunkOpenTelemetry(options =>
        {
            options.EnableTracing = false;
            options.EnableMetrics = false;
        });
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value;

        options.IncludeScopes.Should().BeFalse();
        options.IncludeFormattedMessage.Should().BeTrue();
    }

    private sealed class CaptureExporter<T>(Action<T> capture) : BaseExporter<T>
        where T : class
    {
        public override ExportResult Export(in Batch<T> batch)
        {
            foreach (var item in batch)
            {
                capture(item);
            }

            return ExportResult.Success;
        }
    }
}
