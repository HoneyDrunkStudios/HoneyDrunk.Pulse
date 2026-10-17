// <copyright file="OtlpEndpointsLog.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Endpoints;

/// <summary>
/// LoggerMessage source-generated logging methods for OtlpEndpoints.
/// </summary>
internal static partial class OtlpEndpointsLog
{
    [LoggerMessage(
        EventId = 400,
        Level = LogLevel.Error,
        Message = "Error handling traces request ({ExceptionType})")]
    public static partial void LogTracesRequestError(this ILogger logger, string exceptionType);

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Error,
        Message = "Error handling metrics request ({ExceptionType})")]
    public static partial void LogMetricsRequestError(this ILogger logger, string exceptionType);

    [LoggerMessage(
        EventId = 402,
        Level = LogLevel.Error,
        Message = "Error handling logs request ({ExceptionType})")]
    public static partial void LogLogsRequestError(this ILogger logger, string exceptionType);

    [LoggerMessage(
        EventId = 403,
        Level = LogLevel.Warning,
        Message = "Invalid JSON in analytics request ({ExceptionType})")]
    public static partial void LogAnalyticsInvalidJson(this ILogger logger, string exceptionType);

    [LoggerMessage(
        EventId = 404,
        Level = LogLevel.Error,
        Message = "Error handling analytics request ({ExceptionType})")]
    public static partial void LogAnalyticsRequestError(this ILogger logger, string exceptionType);

    [LoggerMessage(
        EventId = 405,
        Level = LogLevel.Warning,
        Message = "Invalid JSON in error report request ({ExceptionType})")]
    public static partial void LogErrorReportInvalidJson(this ILogger logger, string exceptionType);

    [LoggerMessage(
        EventId = 406,
        Level = LogLevel.Error,
        Message = "Error handling error report request ({ExceptionType})")]
    public static partial void LogErrorReportRequestError(this ILogger logger, string exceptionType);
}
