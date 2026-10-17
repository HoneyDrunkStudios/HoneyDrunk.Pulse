// <copyright file="SinkValidationLog.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// Logger message definitions for sink validation.
/// </summary>
internal static partial class SinkValidationLog
{
    [LoggerMessage(
        EventId = 510,
        Level = LogLevel.Critical,
        Message = "FAIL-FAST: {Message}. Application cannot start in production without required sink endpoints.")]
    public static partial void LogSinkValidationFailFast(this ILogger logger, string message);

    [LoggerMessage(
        EventId = 511,
        Level = LogLevel.Warning,
        Message = "{Message}. Sinks requiring these endpoints may not function correctly.")]
    public static partial void LogMissingSinkEndpointsWarning(this ILogger logger, string message);

    [LoggerMessage(
        EventId = 512,
        Level = LogLevel.Information,
        Message = "All enabled sink endpoints are validated.")]
    public static partial void LogSinkEndpointsValidated(this ILogger logger);
}
