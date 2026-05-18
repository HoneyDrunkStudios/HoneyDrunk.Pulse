// <copyright file="TempoSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Shared;
using HoneyDrunk.Telemetry.Sink.Tempo.Options;
using HoneyDrunk.Vault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Telemetry.Sink.Tempo.Implementation;

/// <summary>
/// HTTP-based Tempo trace sink implementation that forwards OTLP trace data to Tempo.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TempoSink"/> class.
/// </remarks>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="secretStore">The Vault secret store.</param>
/// <param name="options">The Tempo sink options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class TempoSink(
    HttpClient httpClient,
    ISecretStore secretStore,
    IOptions<TempoSinkOptions> options,
    ILogger<TempoSink> logger) : ITraceSink, IDisposable
{
    private readonly TempoSinkOptions _options = options.Value;
    private bool _disposed;

    /// <inheritdoc />
    public async Task ExportAsync(
        ReadOnlyMemory<byte> traceData,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await HttpOtlpSinkExporter.ExportAsync(
            httpClient,
            secretStore,
            new HttpOtlpSinkOptionsAdapter(
                _options.Endpoint,
                _options.Enabled,
                _options.TimeoutSeconds,
                _options.MaxRetries,
                _options.Headers,
                _options.BasicAuthSecretName,
                _options.UsernameSecretName,
                _options.PasswordSecretName),
            traceData,
            contentType,
            new HttpOtlpSinkLogCallbacks(
                LogTracesExported,
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
        Message = "Exported {ByteCount} bytes of trace data to Tempo")]
    private partial void LogTracesExported(int byteCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to export traces to Tempo. Status: {StatusCode}, Response: {Response}")]
    private partial void LogExportFailed(int statusCode, string response);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Retrying Tempo export (attempt {Attempt}/{MaxRetries}): {Error}")]
    private partial void LogExportRetry(int attempt, int maxRetries, string error);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Tempo endpoint not configured, skipping export")]
    private partial void LogEndpointNotConfigured();
}
