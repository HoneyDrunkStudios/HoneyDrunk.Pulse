// <copyright file="OtlpParserTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using FluentAssertions;
using HoneyDrunk.Pulse.Collector.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Tests for the OtlpParser class to verify accurate JSON parsing.
/// </summary>
public class OtlpParserTests
{
    private readonly OtlpParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpParserTests"/> class.
    /// </summary>
    public OtlpParserTests()
    {
        _parser = new OtlpParser(NullLogger<OtlpParser>.Instance);
    }

    /// <summary>
    /// Tests that JSON trace payloads are parsed to count spans accurately.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithJsonPayload_CountsSpansAccurately()
    {
        // Arrange - OTLP JSON trace structure with 2 spans
        var json = """
            {
                "resourceSpans": [{
                    "resource": {
                        "attributes": [{
                            "key": "service.name",
                            "value": { "stringValue": "test-service" }
                        }]
                    },
                    "scopeSpans": [{
                        "spans": [
                            { "traceId": "abc123", "spanId": "span1", "name": "GET /api" },
                            { "traceId": "abc123", "spanId": "span2", "name": "DB Query" }
                        ]
                    }]
                }]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert
        result.SpanCount.Should().Be(2);
        result.ResourceNames.Should().Contain("test-service");
    }

    /// <summary>
    /// Tests that JSON metrics payloads are parsed to count metrics accurately.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseMetricsAsync_WithJsonPayload_CountsMetricsAccurately()
    {
        // Arrange - OTLP JSON metrics structure with 3 metrics
        var json = """
            {
                "resourceMetrics": [{
                    "resource": {
                        "attributes": [{
                            "key": "service.name",
                            "value": { "stringValue": "metrics-service" }
                        }]
                    },
                    "scopeMetrics": [{
                        "metrics": [
                            { "name": "http.requests", "unit": "1" },
                            { "name": "http.duration", "unit": "ms" },
                            { "name": "cpu.usage", "unit": "%" }
                        ]
                    }]
                }]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseMetricsAsync(stream, "application/json");

        // Assert
        result.MetricCount.Should().Be(3);
        result.ResourceNames.Should().Contain("metrics-service");
    }

    /// <summary>
    /// Tests that JSON logs payloads are parsed to count log records accurately.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseLogsAsync_WithJsonPayload_CountsLogsAccurately()
    {
        // Arrange - OTLP JSON logs structure with 4 log records
        var json = """
            {
                "resourceLogs": [{
                    "resource": {
                        "attributes": [{
                            "key": "service.name",
                            "value": { "stringValue": "logging-service" }
                        }]
                    },
                    "scopeLogs": [{
                        "logRecords": [
                            { "severityText": "INFO", "body": { "stringValue": "Log 1" } },
                            { "severityText": "WARN", "body": { "stringValue": "Log 2" } },
                            { "severityText": "ERROR", "body": { "stringValue": "Log 3" } },
                            { "severityText": "DEBUG", "body": { "stringValue": "Log 4" } }
                        ]
                    }]
                }]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseLogsAsync(stream, "application/json");

        // Assert
        result.LogRecordCount.Should().Be(4);
        result.ResourceNames.Should().Contain("logging-service");
    }

    /// <summary>
    /// Tests that protobuf payloads use heuristic-based span counting.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithProtobufContentType_UsesHeuristic()
    {
        // Arrange - Simulate protobuf payload (900 bytes should give ~3 spans at 300 bytes/span)
        var protobufBytes = new byte[900];

        using var stream = new MemoryStream(protobufBytes);

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/x-protobuf");

        // Assert
        result.SpanCount.Should().Be(3); // 900 / 300 = 3
    }

    /// <summary>
    /// Tests that an empty stream returns an empty result.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithEmptyStream_ReturnsEmptyResult()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert
        result.Should().Be(OtlpTraceResult.Empty);
    }

    /// <summary>
    /// Tests that multiple resource spans are counted correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithMultipleResourceSpans_CountsAllSpans()
    {
        // Arrange - Multiple resource spans with multiple scope spans
        var json = """
            {
                "resourceSpans": [
                    {
                        "resource": { "attributes": [{ "key": "service.name", "value": { "stringValue": "service-a" } }] },
                        "scopeSpans": [
                            { "spans": [{ "name": "span1" }, { "name": "span2" }] },
                            { "spans": [{ "name": "span3" }] }
                        ]
                    },
                    {
                        "resource": { "attributes": [{ "key": "service.name", "value": { "stringValue": "service-b" } }] },
                        "scopeSpans": [
                            { "spans": [{ "name": "span4" }, { "name": "span5" }] }
                        ]
                    }
                ]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert
        result.SpanCount.Should().Be(5);
        result.ResourceNames.Should().HaveCount(2);
        result.ResourceNames.Should().Contain("service-a");
        result.ResourceNames.Should().Contain("service-b");
    }

    /// <summary>
    /// Tests that invalid JSON falls back to heuristic counting.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithInvalidJson_FallsBackToHeuristic()
    {
        // Arrange - Invalid JSON that will cause parsing to fail
        var invalidJson = "{ invalid json }}}";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert - Should still return a result (fallback to heuristic)
        result.SpanCount.Should().BeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Tests that error spans are extracted from trace data.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithErrorSpan_ExtractsError()
    {
        // Arrange - Span with error status (code=2)
        var json = """
            {
                "resourceSpans": [{
                    "resource": {
                        "attributes": [{ "key": "service.name", "value": { "stringValue": "test-service" } }]
                    },
                    "scopeSpans": [{
                        "spans": [{
                            "traceId": "abc123",
                            "spanId": "span1",
                            "name": "GET /api",
                            "status": { "code": 2, "message": "Internal error" }
                        }]
                    }]
                }]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert
        result.ErrorSpans.Should().HaveCount(1);
        result.ErrorSpans[0].SpanName.Should().Be("GET /api");
        result.ErrorSpans[0].ServiceName.Should().Be("test-service");
        result.ErrorSpans[0].ErrorMessage.Should().Be("Internal error");
        result.ErrorSpans[0].TraceId.Should().Be("abc123");
        result.ErrorSpans[0].SpanId.Should().Be("span1");
    }

    /// <summary>
    /// Tests that exception event details are extracted from error spans.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithExceptionEvent_ExtractsExceptionDetails()
    {
        // Arrange - Span with exception event
        var json = """
            {
                "resourceSpans": [{
                    "resource": {
                        "attributes": [{ "key": "service.name", "value": { "stringValue": "test-service" } }]
                    },
                    "scopeSpans": [{
                        "spans": [{
                            "traceId": "abc123",
                            "spanId": "span1",
                            "name": "GET /api",
                            "status": { "code": 2 },
                            "events": [{
                                "name": "exception",
                                "attributes": [
                                    { "key": "exception.type", "value": { "stringValue": "System.ArgumentException" } },
                                    { "key": "exception.message", "value": { "stringValue": "Value cannot be null" } },
                                    { "key": "exception.stacktrace", "value": { "stringValue": "at Test.Method()" } }
                                ]
                            }]
                        }]
                    }]
                }]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert
        result.ErrorSpans.Should().HaveCount(1);
        result.ErrorSpans[0].ExceptionType.Should().Be("System.ArgumentException");
        result.ErrorSpans[0].ExceptionMessage.Should().Be("Value cannot be null");
        result.ErrorSpans[0].StackTrace.Should().Be("at Test.Method()");
    }

    /// <summary>
    /// Tests that spans with OK status do not produce error spans.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithOkSpan_ReturnsEmptyErrorSpans()
    {
        // Arrange - Span with OK status (code=1) or no error
        var json = """
            {
                "resourceSpans": [{
                    "resource": {
                        "attributes": [{ "key": "service.name", "value": { "stringValue": "test-service" } }]
                    },
                    "scopeSpans": [{
                        "spans": [{
                            "traceId": "abc123",
                            "spanId": "span1",
                            "name": "GET /api",
                            "status": { "code": 1 }
                        }]
                    }]
                }]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert
        result.ErrorSpans.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that span attributes are included in extracted error spans.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithSpanAttributes_IncludesAttributesInErrors()
    {
        // Arrange - Error span with attributes
        var json = """
            {
                "resourceSpans": [{
                    "resource": {
                        "attributes": [{ "key": "service.name", "value": { "stringValue": "test-service" } }]
                    },
                    "scopeSpans": [{
                        "spans": [{
                            "traceId": "abc123",
                            "spanId": "span1",
                            "name": "GET /api",
                            "status": { "code": 2 },
                            "attributes": [
                                { "key": "http.method", "value": { "stringValue": "GET" } },
                                { "key": "http.status_code", "value": { "intValue": 500 } }
                            ]
                        }]
                    }]
                }]
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _parser.ParseTracesAsync(stream, "application/json");

        // Assert
        result.ErrorSpans.Should().HaveCount(1);
        result.ErrorSpans[0].Attributes.Should().ContainKey("http.method");
        result.ErrorSpans[0].Attributes["http.method"].Should().Be("GET");
        result.ErrorSpans[0].Attributes.Should().ContainKey("http.status_code");
        result.ErrorSpans[0].Attributes["http.status_code"].Should().Be("500");
    }
}
