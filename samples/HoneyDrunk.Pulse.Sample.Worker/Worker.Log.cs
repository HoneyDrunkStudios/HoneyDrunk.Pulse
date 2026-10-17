// <copyright file="Worker.Log.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Sample.Worker;

/// <summary>
/// LoggerMessage source-generated logging methods for Worker.
/// </summary>
public sealed partial class Worker
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Worker running job {JobId} at: {Time}")]
    private partial void LogJobRunning(int jobId, DateTimeOffset time);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Job {JobId} completed - emitted analytics event: {EventName}")]
    private partial void LogJobCompleted(int jobId, string eventName);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Error processing job {JobId}")]
    private partial void LogJobError(Exception ex, int jobId);
}
