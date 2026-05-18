// <copyright file="LokiSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Loki.Options;
using HoneyDrunk.Telemetry.Sink.Shared;
using HoneyDrunk.Vault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Telemetry.Sink.Loki.Implementation;

/// <summary>
/// HTTP-based Loki log sink implementation that forwards OTLP log data to Loki.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LokiSink"/> class.
/// </remarks>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="secretStore">The Vault secret store.</param>
/// <param name="options">The Loki sink options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class LokiSink(
    HttpClient httpClient,
    ISecretStore secretStore,
    IOptions<LokiSinkOptions> options,
    ILogger<LokiSink> logger) : ILogSink, IDisposable
{
    private readonly LokiSinkOptions _options = options.Value;
    private bool _disposed;

    /// <inheritdoc />
    public async Task ExportAsync(
        ReadOnlyMemory<byte> logData,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await HttpOtlpSinkExporter.ExportAsync(
            httpClient,
            secretStore,
            _options,
            logData,
            contentType,
            new HttpOtlpSinkLogCallbacks(
                LogLogsExported,
                LogExportFailed,
                LogExportRetry,
                LogEndpointNotConfigured),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // HTTP exports are immediate, no buffering to flush
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Exported {ByteCount} bytes of log data to Loki")]
    private partial void LogLogsExported(int byteCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to export logs to Loki. Status: {StatusCode}, Response: {Response}")]
    private partial void LogExportFailed(int statusCode, string response);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Retrying Loki export (attempt {Attempt}/{MaxRetries}): {Error}")]
    private partial void LogExportRetry(int attempt, int maxRetries, string error);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Loki endpoint not configured, skipping export")]
    private partial void LogEndpointNotConfigured();
}
