// <copyright file="OtlpPayloadRedactorTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using Google.Protobuf;
using HoneyDrunk.Pulse.Collector.Ingestion;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using System.Text;
using System.Text.Json;
using Span = OpenTelemetry.Proto.Trace.V1.Span;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>Regression coverage for the collector export redaction boundary.</summary>
public class OtlpPayloadRedactorTests
{
    /// <summary>Redacts JSON attributes and bodies without changing OTLP trace identifiers.</summary>
    /// <param name="signal">The signal sanitizer to exercise.</param>
    [Theory]
    [InlineData("traces")]
    [InlineData("metrics")]
    [InlineData("logs")]
    public void Json_RedactsNestedValuesAndPreservesIdentifiers(string signal)
    {
        const string payload = """
            {"resourceLogs":[{"resource":{"attributes":[{"key":"Authorization","value":{"stringValue":"private-value"}}]},
            "scopeLogs":[{"logRecords":[{"traceId":"0123456789abcdef0123456789abcdef","spanId":"0123456789abcdef",
            "body":{"kvlistValue":{"values":[{"key":"password","value":{"stringValue":"private-password"}},
            {"key":"message","value":{"stringValue":"contact reader@example.test"}}]}},
            "attributes":[{"key":"payload","value":{"bytesValue":"cHJpdmF0ZQ=="}},{"key":"ok","value":{"intValue":"7"}}]}]}]}]}
            """;
        var output = Encoding.UTF8.GetString(Redact(signal, Encoding.UTF8.GetBytes(payload), "Application/Json; charset=utf-8").Span);
        output.Should().NotContain("private-value").And.NotContain("private-password").And.NotContain("reader@example.test").And.NotContain("cHJpdmF0ZQ==");
        output.Should().Contain("0123456789abcdef0123456789abcdef").And.Contain("0123456789abcdef").And.Contain("[REDACTED]");
        using var json = JsonDocument.Parse(output);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    /// <summary>Redacts resource, scope, event and link attributes in protobuf traces.</summary>
    [Fact]
    public void ProtobufTraces_RedactsAllAttributeLocations()
    {
        var span = new Span
        {
            Name = "password=private-name",
            TraceId = ByteString.CopyFrom(new byte[16]),
            SpanId = ByteString.CopyFrom(new byte[8]),
            Status = new Status { Message = "email reader@example.test", Code = StatusCode.Error },
        };
        span.Attributes.Add(Secret());
        var spanEvent = new Event { Name = "failure" };
        spanEvent.Attributes.Add(Secret());
        span.Events.Add(spanEvent);
        var link = new Link { TraceId = span.TraceId, SpanId = span.SpanId };
        link.Attributes.Add(Secret());
        span.Links.Add(link);
        var scope = new ScopeSpans { Scope = new InstrumentationScope() };
        scope.Scope.Attributes.Add(Secret());
        scope.Spans.Add(span);
        var resource = new ResourceSpans { Resource = new Resource() };
        resource.Resource.Attributes.Add(Secret());
        resource.ScopeSpans.Add(scope);
        var request = new ExportTraceServiceRequest();
        request.ResourceSpans.Add(resource);

        var output = ExportTraceServiceRequest.Parser.ParseFrom(OtlpPayloadRedactor.RedactTraces(request.ToByteArray(), "application/x-protobuf").Span);

        output.ToString().Should().NotContain("private-").And.NotContain("reader@example.test");
        output.ResourceSpans[0].ScopeSpans[0].Spans[0].TraceId.Should().Equal(span.TraceId);
        output.ResourceSpans[0].ScopeSpans[0].Spans[0].SpanId.Should().Equal(span.SpanId);
        request.ResourceSpans[0].Resource.Attributes[0].Value.StringValue.Should().Be("private-secret");
    }

    /// <summary>Redacts binary log bodies and nested key/value lists.</summary>
    [Fact]
    public void ProtobufLogs_RedactsBodiesAndNestedValues()
    {
        var values = new KeyValueList();
        values.Values.Add(Secret());
        var record = new LogRecord { Body = new AnyValue { KvlistValue = values } };
        var scope = new ScopeLogs();
        scope.LogRecords.Add(record);
        scope.LogRecords.Add(new LogRecord { Body = new AnyValue { BytesValue = ByteString.CopyFromUtf8("private-binary") } });
        var resource = new ResourceLogs();
        resource.ScopeLogs.Add(scope);
        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(resource);

        var output = ExportLogsServiceRequest.Parser.ParseFrom(OtlpPayloadRedactor.RedactLogs(request.ToByteArray(), "application/x-protobuf").Span);

        output.ToString().Should().NotContain("private-");
        output.ResourceLogs[0].ScopeLogs[0].LogRecords[1].Body.StringValue.Should().Be("[REDACTED]");
    }

    /// <summary>Sanitizes metrics attributes and exemplar attributes without dropping measurements.</summary>
    [Fact]
    public void ProtobufMetrics_PreservesValuesAndSanitizesExemplars()
    {
        var point = new NumberDataPoint { AsInt = 42 };
        point.Attributes.Add(Secret());
        var exemplar = new Exemplar { AsInt = 3, TraceId = ByteString.CopyFrom(new byte[16]) };
        exemplar.FilteredAttributes.Add(Secret());
        point.Exemplars.Add(exemplar);
        var metric = new Metric { Name = "request.count", Gauge = new Gauge() };
        metric.Gauge.DataPoints.Add(point);
        var scope = new ScopeMetrics();
        scope.Metrics.Add(metric);
        var resource = new ResourceMetrics();
        resource.ScopeMetrics.Add(scope);
        var request = new ExportMetricsServiceRequest();
        request.ResourceMetrics.Add(resource);

        var output = ExportMetricsServiceRequest.Parser.ParseFrom(OtlpPayloadRedactor.RedactMetrics(request.ToByteArray(), "application/x-protobuf").Span);

        output.ToString().Should().NotContain("private-secret");
        output.ResourceMetrics[0].ScopeMetrics[0].Metrics[0].Gauge.DataPoints[0].AsInt.Should().Be(42);
        output.ResourceMetrics[0].ScopeMetrics[0].Metrics[0].Gauge.DataPoints[0].Exemplars[0].TraceId.Should().Equal(exemplar.TraceId);
    }

    /// <summary>Opaque unknown protobuf fields are dropped rather than bypassing redaction.</summary>
    [Fact]
    public void Protobuf_DiscardsUnknownFields()
    {
        byte[] unknownField = [0xA2, 0x06, 0x06, 0x73, 0x65, 0x63, 0x72, 0x65, 0x74];
        OtlpPayloadRedactor.RedactTraces(unknownField, "application/x-protobuf").IsEmpty.Should().BeTrue();
    }

    /// <summary>Malformed or unsupported inputs are rejected without returning original bytes.</summary>
    /// <param name="contentType">The input content type.</param>
    /// <param name="data">The invalid input.</param>
    [Theory]
    [InlineData("application/json", "not json")]
    [InlineData("application/json", "[]")]
    [InlineData("application/x-protobuf", "broken")]
    [InlineData("text/plain", "password=private")]
    public void InvalidPayload_FailsClosed(string contentType, string data)
    {
        var act = () => OtlpPayloadRedactor.RedactTraces(Encoding.UTF8.GetBytes(data), contentType);
        act.Should().Throw<InvalidDataException>();
    }

    /// <summary>Excessively nested input is rejected without exporting unsanitized data.</summary>
    [Fact]
    public void Json_RejectsExcessiveNesting()
    {
        var payload = string.Concat(Enumerable.Repeat("{\"nested\":", 40)) + "0" + new string('}', 40);
        var act = () => OtlpPayloadRedactor.RedactLogs(Encoding.UTF8.GetBytes(payload), "application/json");
        act.Should().Throw<InvalidDataException>();
    }

    private static KeyValue Secret() => new() { Key = "api.key", Value = new AnyValue { StringValue = "private-secret" } };

    private static ReadOnlyMemory<byte> Redact(string signal, byte[] payload, string contentType) => signal switch
    {
        "traces" => OtlpPayloadRedactor.RedactTraces(payload, contentType),
        "metrics" => OtlpPayloadRedactor.RedactMetrics(payload, contentType),
        _ => OtlpPayloadRedactor.RedactLogs(payload, contentType),
    };
}
