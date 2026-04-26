// <copyright file="SecretValidationLog.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Pulse.Collector.Configuration;

/// <summary>
/// LoggerMessage source-generated logging methods for secret validation.
/// </summary>
internal static partial class SecretValidationLog
{
    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Critical,
        Message = "FAIL-FAST: {Message}. Application cannot start in production without required secrets.")]
    public static partial void LogFailFast(this ILogger logger, string message);

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Warning,
        Message = "{Message}. Sinks requiring these secrets may not function correctly.")]
    public static partial void LogMissingSecretWarning(this ILogger logger, string message);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Information,
        Message = "All required secrets validated successfully via Vault.")]
    public static partial void LogSecretsValidated(this ILogger logger);
}
