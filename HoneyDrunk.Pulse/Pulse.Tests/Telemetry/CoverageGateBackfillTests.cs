// <copyright file="CoverageGateBackfillTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using Google.Protobuf;
using HoneyDrunk.Pulse.Collector.Ingestion;
using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Abstractions.Extensions;
using HoneyDrunk.Telemetry.Abstractions.Models;
using HoneyDrunk.Telemetry.Abstractions.Tags;
using HoneyDrunk.Telemetry.OpenTelemetry.Enrichment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using System.Diagnostics;
using System.Text;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Focused tests for telemetry helpers and OTLP parser branches that protect coverage-gate behavior.
/// </summary>
public sealed class CoverageGateBackfillTests
{
    /// <summary>
    /// Verifies activity enrichment writes only supplied node identity tags and tolerates null activities.
    /// </summary>
    [Fact]
    public void ActivityEnricher_EnrichesOptionalStringContexts()
    {
        // Arrange
        using var activity = new Activity("pulse-test");
        activity.Start();

        // Act
        ActivityEnricher.EnrichWithNodeIdentity(activity, "pulse", "Pulse", "ops");
        ActivityEnricher.EnrichWithGridContext(activity, "grid-1", "tenant-1");
        ActivityEnricher.EnrichWithUserContext(activity, "user-1", "session-1");
        ActivityEnricher.EnrichWithEnvironment(activity, "ci", "1.2.3");
        ActivityEnricher.EnrichWithNodeIdentity(null, "ignored", "ignored", "ignored");
        ActivityEnricher.EnrichWithGridContext(null, "ignored", "ignored");
        ActivityEnricher.EnrichWithUserContext(null, "ignored", "ignored");
        ActivityEnricher.EnrichWithEnvironment(null, "ignored", "ignored");

        // Assert
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.NodeId && tag.Value == "pulse");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.NodeName && tag.Value == "Pulse");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.NodeType && tag.Value == "ops");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.GridId && tag.Value == "grid-1");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.TenantId && tag.Value == "tenant-1");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.UserId && tag.Value == "user-1");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.SessionId && tag.Value == "session-1");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.Environment && tag.Value == "ci");
        activity.Tags.Should().Contain(tag => tag.Key == TelemetryTagKeys.HoneyDrunk.Version && tag.Value == "1.2.3");
    }

    /// <summary>
    /// Verifies event tag projection includes all canonical tags and custom properties.
    /// </summary>
    [Fact]
    public void TelemetryEventExtensions_ToTagDictionary_ProjectsCanonicalAndCustomTags()
    {
        // Arrange
        var telemetryEvent = TelemetryEvent.Create("checkout.completed")
            .WithDistinctId("distinct-1")
            .WithCorrelationId("corr-1")
            .WithProperty("custom", 42);
        telemetryEvent.OperationId = "op-1";
        telemetryEvent.NodeId = "pulse";
        telemetryEvent.NodeName = "Pulse";
        telemetryEvent.GridId = "grid-1";
        telemetryEvent.TenantId = "tenant-1";
        telemetryEvent.UserId = "user-1";
        telemetryEvent.SessionId = "session-1";
        telemetryEvent.Environment = "ci";

        // Act
        var tags = telemetryEvent.ToTagDictionary();
        telemetryEvent.WithOperationContext(null).WithGridContext(null).WithNodeContext(null);

        // Assert
        tags[TelemetryTagKeys.HoneyDrunk.NodeId].Should().Be("pulse");
        tags[TelemetryTagKeys.HoneyDrunk.NodeName].Should().Be("Pulse");
        tags[TelemetryTagKeys.HoneyDrunk.CorrelationId].Should().Be("corr-1");
        tags[TelemetryTagKeys.HoneyDrunk.OperationId].Should().Be("op-1");
        tags[TelemetryTagKeys.HoneyDrunk.GridId].Should().Be("grid-1");
        tags[TelemetryTagKeys.HoneyDrunk.TenantId].Should().Be("tenant-1");
        tags[TelemetryTagKeys.HoneyDrunk.UserId].Should().Be("user-1");
        tags[TelemetryTagKeys.HoneyDrunk.SessionId].Should().Be("session-1");
        tags[TelemetryTagKeys.HoneyDrunk.Environment].Should().Be("ci");
        tags["custom"].Should().Be(42);
    }

    /// <summary>
    /// Verifies sink registration extensions expose the expected sink abstractions.
    /// </summary>
    [Fact]
    public void SinkRegistrationExtensions_RegisterExpectedAbstractions()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telemetry:Sinks:Loki:Endpoint"] = "https://loki.example.test",
                ["Telemetry:Sinks:Mimir:Endpoint"] = "https://mimir.example.test",
                ["Telemetry:Sinks:Tempo:Endpoint"] = "https://tempo.example.test",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        HoneyDrunk.Telemetry.Sink.Loki.Extensions.ServiceCollectionExtensions.AddLokiSink(
            services,
            configuration);
        HoneyDrunk.Telemetry.Sink.Mimir.Extensions.ServiceCollectionExtensions.AddMimirSink(
            services,
            options => options.Endpoint = "https://mimir.example.test");
        HoneyDrunk.Telemetry.Sink.Tempo.Extensions.ServiceCollectionExtensions.AddTempoSink(
            services,
            options => options.Endpoint = "https://tempo.example.test");
        HoneyDrunk.Telemetry.Sink.AzureMonitor.Extensions.ServiceCollectionExtensions.AddAzureMonitorSink(
            services,
            _ => { },
            "InstrumentationKey=test");

        // Assert
        services.Count(descriptor => descriptor.ServiceType == typeof(ILogSink)).Should().Be(2);
        services.Count(descriptor => descriptor.ServiceType == typeof(IMetricsSink)).Should().Be(2);
        services.Count(descriptor => descriptor.ServiceType == typeof(ITraceSink)).Should().Be(2);
    }

    /// <summary>
    /// Verifies direct protobuf trace parsing extracts resource names, error status, exceptions, IDs, and attributes.
    /// </summary>
    [Fact]
    public void OtlpParser_ParseTracesFromProto_ExtractsErrorSpanDetails()
    {
        // Arrange
        var parser = new OtlpParser(NullLogger<OtlpParser>.Instance);
        var request = new ExportTraceServiceRequest();
        request.ResourceSpans.Add(new ResourceSpans
        {
            Resource = Resource("checkout-service"),
            ScopeSpans =
            {
                new ScopeSpans
                {
                    Spans =
                    {
                        new Span
                        {
                            Name = "POST /checkout",
                            TraceId = ByteString.CopyFrom(Convert.FromHexString("00112233445566778899aabbccddeeff")),
                            SpanId = ByteString.CopyFrom(Convert.FromHexString("0011223344556677")),
                            Status = new Status { Code = StatusCode.Error, Message = "boom" },
                            Attributes = { Attribute("http.status_code", 500), Attribute("retryable", true) },
                            Events =
                            {
                                new OpenTelemetry.Proto.Trace.V1.Event
                                {
                                    Name = "exception",
                                    Attributes =
                                    {
                                        Attribute("exception.type", "InvalidOperationException"),
                                        Attribute("exception.message", "bad state"),
                                        Attribute("exception.stacktrace", "at Checkout()"),
                                    },
                                },
                            },
                        },
                        new Span { Name = "cache lookup", Status = new Status { Code = StatusCode.Ok } },
                    },
                },
            },
        });

        // Act
        var result = parser.ParseTracesFromProto(request);

        // Assert
        result.SpanCount.Should().Be(2);
        result.ResourceNames.Should().ContainSingle().Which.Should().Be("checkout-service");
        result.ErrorSpans.Should().ContainSingle();
        var error = result.ErrorSpans[0];
        error.SpanName.Should().Be("POST /checkout");
        error.ServiceName.Should().Be("checkout-service");
        error.ErrorMessage.Should().Be("boom");
        error.ExceptionType.Should().Be("InvalidOperationException");
        error.ExceptionMessage.Should().Be("bad state");
        error.StackTrace.Should().Be("at Checkout()");
        error.TraceId.Should().Be("00112233445566778899aabbccddeeff");
        error.SpanId.Should().Be("0011223344556677");
        error.Attributes["http.status_code"].Should().Be("500");
        error.Attributes["retryable"].Should().Be(bool.TrueString);
    }

    /// <summary>
    /// Verifies protobuf trace bytes follow the same extraction path as direct protobuf parsing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task OtlpParser_ParseTracesAsync_WithProtobufBytes_ExtractsResourceAndErrors()
    {
        // Arrange
        var parser = new OtlpParser(NullLogger<OtlpParser>.Instance);
        var request = new ExportTraceServiceRequest();
        request.ResourceSpans.Add(new ResourceSpans
        {
            Resource = Resource("api-service"),
            ScopeSpans =
            {
                new ScopeSpans
                {
                    Spans =
                    {
                        new Span
                        {
                            Name = "GET /api",
                            Status = new Status { Code = StatusCode.Error, Message = "failed" },
                        },
                    },
                },
            },
        });
        using var stream = new MemoryStream(request.ToByteArray());

        // Act
        var result = await parser.ParseTracesAsync(stream, "application/x-protobuf");

        // Assert
        result.SpanCount.Should().BeGreaterThanOrEqualTo(1);
        result.ResourceNames.Should().Contain("api-service");
        result.ErrorSpans.Should().ContainSingle(span => span.SpanName == "GET /api" && span.ErrorMessage == "failed");
    }

    /// <summary>
    /// Verifies direct protobuf log parsing extracts resource names, severity, exception details, and IDs.
    /// </summary>
    [Fact]
    public void OtlpParser_ParseLogsFromProto_ExtractsErrorLogDetails()
    {
        // Arrange
        var parser = new OtlpParser(NullLogger<OtlpParser>.Instance);
        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(new ResourceLogs
        {
            Resource = Resource("worker-service"),
            ScopeLogs =
            {
                new ScopeLogs
                {
                    LogRecords =
                    {
                        new LogRecord
                        {
                            SeverityNumber = SeverityNumber.Error,
                            SeverityText = "ERROR",
                            TimeUnixNano = 1_700_000_000_000_000_000,
                            TraceId = ByteString.CopyFrom(Convert.FromHexString("11112222333344445555666677778888")),
                            SpanId = ByteString.CopyFrom(Convert.FromHexString("9999aaaabbbbcccc")),
                            Body = new AnyValue { StringValue = "job failed" },
                            Attributes =
                            {
                                Attribute("exception.type", "TimeoutException"),
                                Attribute("exception.message", "timeout"),
                                Attribute("attempt", 3),
                            },
                        },
                        new LogRecord
                        {
                            SeverityNumber = SeverityNumber.Info,
                            SeverityText = "INFO",
                            Body = new AnyValue { StringValue = "job started" },
                        },
                    },
                },
            },
        });

        // Act
        var result = parser.ParseLogsFromProto(request);

        // Assert
        result.LogRecordCount.Should().Be(2);
        result.MaxSeverityNumber.Should().Be((int)SeverityNumber.Error);
        result.ResourceNames.Should().ContainSingle().Which.Should().Be("worker-service");
        result.ErrorLogs.Should().ContainSingle();
        var error = result.ErrorLogs[0];
        error.Message.Should().Be("job failed");
        error.SeverityText.Should().Be("ERROR");
        error.ServiceName.Should().Be("worker-service");
        error.TraceId.Should().Be("11112222333344445555666677778888");
        error.SpanId.Should().Be("9999aaaabbbbcccc");
        error.ExceptionType.Should().Be("TimeoutException");
        error.ExceptionMessage.Should().Be("timeout");
        error.Attributes["attempt"].Should().Be("3");
    }

    /// <summary>
    /// Verifies JSON log parsing recognizes error severity text and exception attributes.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task OtlpParser_ParseLogsAsync_WithJsonErrorLog_ExtractsSeverityAndException()
    {
        // Arrange
        var parser = new OtlpParser(NullLogger<OtlpParser>.Instance);
        const string Json = """
            {
              "resourceLogs": [{
                "resource": { "attributes": [{ "key": "service.name", "value": { "stringValue": "json-worker" } }] },
                "scopeLogs": [{
                  "logRecords": [{
                    "severityNumber": 9,
                    "severityText": "CRITICAL",
                    "timeUnixNano": 1700000000000000000,
                    "traceId": "abc",
                    "spanId": "def",
                    "body": { "stringValue": "critical failure" },
                    "attributes": [
                      { "key": "exception.type", "value": { "stringValue": "JsonException" } },
                      { "key": "exception.message", "value": { "stringValue": "bad json" } },
                      { "key": "feature.enabled", "value": { "boolValue": true } }
                    ]
                  }]
                }]
              }]
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Json));

        // Act
        var result = await parser.ParseLogsAsync(stream, "application/json; charset=utf-8");

        // Assert
        result.LogRecordCount.Should().Be(1);
        result.MaxSeverityNumber.Should().Be(9);
        result.ResourceNames.Should().Contain("json-worker");
        result.ErrorLogs.Should().ContainSingle();
        result.ErrorLogs[0].Message.Should().Be("critical failure");
        result.ErrorLogs[0].ExceptionType.Should().Be("JsonException");
        result.ErrorLogs[0].ExceptionMessage.Should().Be("bad json");
        result.ErrorLogs[0].Attributes["feature.enabled"].Should().Be(bool.TrueString);
    }

    private static Resource Resource(string serviceName)
        => new()
        {
            Attributes = { Attribute("service.name", serviceName) },
        };

    private static KeyValue Attribute(string key, string value)
        => new() { Key = key, Value = new AnyValue { StringValue = value } };

    private static KeyValue Attribute(string key, long value)
        => new() { Key = key, Value = new AnyValue { IntValue = value } };

    private static KeyValue Attribute(string key, bool value)
        => new() { Key = key, Value = new AnyValue { BoolValue = value } };
}
