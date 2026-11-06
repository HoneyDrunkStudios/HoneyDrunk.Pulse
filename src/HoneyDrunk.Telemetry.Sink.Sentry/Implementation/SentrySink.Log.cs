// Copyright (c) HoneyDrunk Studios. All rights reserved.

using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Telemetry.Sink.Sentry.Implementation;

/// <summary>
/// LoggerMessage source-generated logging methods for SentrySink.
/// </summary>
public sealed partial class SentrySink
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Sentry sink initialized for environment: {Environment}")]
    private partial void LogSentryInitialized(string? environment);
}
