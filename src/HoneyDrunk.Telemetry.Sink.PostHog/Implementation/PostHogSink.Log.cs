// <copyright file="PostHogSink.Log.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Telemetry.Sink.PostHog.Implementation;

/// <summary>
/// LoggerMessage source-generated logging methods for PostHogSink.
/// </summary>
public partial class PostHogSink
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Successfully sent {EventCount} events to PostHog")]
    private partial void LogEventsSent(int eventCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to send {EventCount} events to PostHog")]
    private partial void LogSendFailed(Exception ex, int eventCount);
}
