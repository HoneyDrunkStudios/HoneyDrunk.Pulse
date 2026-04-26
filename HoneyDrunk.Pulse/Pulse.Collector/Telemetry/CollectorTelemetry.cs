// <copyright file="CollectorTelemetry.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Conventions;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HoneyDrunk.Pulse.Collector.Telemetry;

/// <summary>
/// Telemetry instrumentation for the Pulse Collector.
/// </summary>
public sealed class CollectorTelemetry
{
    /// <summary>
    /// The name of the activity source.
    /// </summary>
    public const string ActivitySourceName = TelemetryNames.ActivitySources.PulseCollector;

    /// <summary>
    /// The name of the meter.
    /// </summary>
    public const string MeterName = TelemetryNames.Meters.PulseCollector;

    /// <summary>
    /// The activity source for the collector.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    /// <summary>
    /// The meter for the collector.
    /// </summary>
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    // Counters
    private static readonly Counter<long> TracesIngestedCounter = Meter.CreateCounter<long>(
        "pulse.collector.traces.ingested",
        "traces",
        "Number of traces ingested");

    private static readonly Counter<long> MetricsIngestedCounter = Meter.CreateCounter<long>(
        "pulse.collector.metrics.ingested",
        "metrics",
        "Number of metrics ingested");

    private static readonly Counter<long> LogsIngestedCounter = Meter.CreateCounter<long>(
        "pulse.collector.logs.ingested",
        "logs",
        "Number of logs ingested");

    private static readonly Counter<long> AnalyticsEventsIngestedCounter = Meter.CreateCounter<long>(
        "pulse.collector.analytics_events.ingested",
        "events",
        "Number of analytics events ingested");

    private static readonly Counter<long> ErrorsCounter = Meter.CreateCounter<long>(
        "pulse.collector.errors",
        "errors",
        "Number of errors during ingestion");

    private static readonly Counter<long> ErrorsForwardedCounter = Meter.CreateCounter<long>(
        "pulse.collector.errors.forwarded",
        "errors",
        "Number of errors forwarded to Sentry from trace spans");

    // Histograms
    private static readonly Histogram<double> ProcessingDurationHistogram = Meter.CreateHistogram<double>(
        "pulse.collector.processing.duration",
        "ms",
        "Processing duration in milliseconds");

    /// <summary>
    /// Records the number of traces ingested.
    /// </summary>
    /// <param name="count">The count.</param>
    /// <param name="sourceName">The source name.</param>
    public static void RecordTracesIngested(long count, string? sourceName = null)
    {
        var tags = CreateTags(sourceName);
        TracesIngestedCounter.Add(count, tags);
    }

    /// <summary>
    /// Records the number of metrics ingested.
    /// </summary>
    /// <param name="count">The count.</param>
    /// <param name="sourceName">The source name.</param>
    public static void RecordMetricsIngested(long count, string? sourceName = null)
    {
        var tags = CreateTags(sourceName);
        MetricsIngestedCounter.Add(count, tags);
    }

    /// <summary>
    /// Records the number of logs ingested.
    /// </summary>
    /// <param name="count">The count.</param>
    /// <param name="sourceName">The source name.</param>
    public static void RecordLogsIngested(long count, string? sourceName = null)
    {
        var tags = CreateTags(sourceName);
        LogsIngestedCounter.Add(count, tags);
    }

    /// <summary>
    /// Records the number of analytics events ingested.
    /// </summary>
    /// <param name="count">The count.</param>
    /// <param name="sourceName">The source name.</param>
    public static void RecordAnalyticsEventsIngested(long count, string? sourceName = null)
    {
        var tags = CreateTags(sourceName);
        AnalyticsEventsIngestedCounter.Add(count, tags);
    }

    /// <summary>
    /// Records an error during ingestion.
    /// </summary>
    /// <param name="errorType">The type of error.</param>
    public static void RecordError(string errorType)
    {
        ErrorsCounter.Add(1, new KeyValuePair<string, object?>("error.type", errorType));
    }

    /// <summary>
    /// Records an error forwarded to Sentry from trace spans.
    /// </summary>
    /// <param name="sourceName">The source service name.</param>
    public static void RecordErrorForwarded(string? sourceName = null)
    {
        var tags = CreateTags(sourceName);
        ErrorsForwardedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records the processing duration.
    /// </summary>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="sourceName">The source name.</param>
    public static void RecordProcessingDuration(double durationMs, string? sourceName = null)
    {
        var tags = CreateTags(sourceName);
        ProcessingDurationHistogram.Record(durationMs, tags);
    }

    /// <summary>
    /// Starts an activity for ingestion processing.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <returns>The started activity, or null if not sampled.</returns>
    public static Activity? StartIngestionActivity(string operationName)
    {
        return ActivitySource.StartActivity(operationName, ActivityKind.Server);
    }

    private static KeyValuePair<string, object?>[] CreateTags(string? sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return [];
        }

        return [new KeyValuePair<string, object?>("source.name", sourceName)];
    }
}
