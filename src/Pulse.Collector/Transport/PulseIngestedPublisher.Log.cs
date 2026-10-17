// <copyright file="PulseIngestedPublisher.Log.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Transport;

/// <summary>
/// LoggerMessage source-generated logging methods for PulseIngestedPublisher.
/// </summary>
public sealed partial class PulseIngestedPublisher
{
    [LoggerMessage(
        EventId = 300,
        Level = LogLevel.Debug,
        Message = "Publishing PulseIngested event: BatchId={BatchId}, Traces={TraceCount}, Metrics={MetricCount}, Logs={LogCount}, Analytics={AnalyticsCount}")]
    private partial void LogPublishingPulseIngested(string batchId, int traceCount, int metricCount, int logCount, int analyticsCount);
}
