// Copyright (c) HoneyDrunk Studios. All rights reserved.

using AwesomeAssertions;
using HoneyDrunk.Pulse.Collector.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Regression coverage for filtering optional OTLP JSON members.
/// </summary>
public class OtlpParserFilteringTests
{
    private readonly OtlpParser _parser = new(NullLogger<OtlpParser>.Instance);

    /// <summary>
    /// Missing resource and scope members do not hide later trace records.
    /// </summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task ParseTracesAsync_WithMissingOptionalMembers_CountsLaterSpans()
    {
        const string json = """
            {"resourceSpans":[{}, {"scopeSpans":[{}, {"spans":[{}, {}]}]}]}
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await _parser.ParseTracesAsync(stream, "application/json");

        result.SpanCount.Should().Be(2);
        result.ErrorSpans.Should().BeEmpty();
    }

    /// <summary>
    /// Missing resource and scope members do not hide later metric records.
    /// </summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task ParseMetricsAsync_WithMissingOptionalMembers_CountsLaterMetrics()
    {
        const string json = """
            {"resourceMetrics":[{}, {"scopeMetrics":[{}, {"metrics":[{}, {}, {}]}]}]}
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await _parser.ParseMetricsAsync(stream, "application/json");

        result.MetricCount.Should().Be(3);
    }

    /// <summary>
    /// Optional attributes and severities preserve valid names, counts and maximum severity.
    /// </summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task ParseLogsAsync_WithOptionalValues_PreservesValidRecords()
    {
        const string json = """
            {"resourceLogs":[{}, {
              "resource":{"attributes":[{},
                {"key":"service.name"},
                {"key":"service.name","value":{}},
                {"key":"service.name","value":{"stringValue":" "}},
                {"key":"service.name","value":{"stringValue":null}},
                {"key":"SERVICE.NAME","value":{"stringValue":"collector"}}]},
              "scopeLogs":[{}, {"logRecords":[{},
                {"severityNumber":9}, {"severityNumber":13}, {"severityNumber":5}]}]
            }]}
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await _parser.ParseLogsAsync(stream, "application/json");

        result.LogRecordCount.Should().Be(4);
        result.ResourceNames.Should().ContainSingle().Which.Should().Be("collector");
        result.MaxSeverityNumber.Should().Be(13);
        result.ErrorLogs.Should().BeEmpty();
    }
}
