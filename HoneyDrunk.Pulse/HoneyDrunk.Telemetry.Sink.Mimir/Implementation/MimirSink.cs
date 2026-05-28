// <copyright file="MimirSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Mimir.Options;
using HoneyDrunk.Telemetry.Sink.Shared;
using HoneyDrunk.Vault.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Telemetry.Sink.Mimir.Implementation;

/// <summary>
/// HTTP-based Mimir metrics sink implementation that forwards OTLP metric data to Mimir.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MimirSink"/> class.
/// </remarks>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="secretStore">The Vault secret store.</param>
/// <param name="options">The Mimir sink options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class MimirSink(
    HttpClient httpClient,
    ISecretStore secretStore,
    IOptions<MimirSinkOptions> options,
    ILogger<MimirSink> logger) : IMetricsSink, IDisposable
{
    private readonly MimirSinkOptions _options = options.Value;
    private bool _disposed;

    /// <inheritdoc />
    public async Task ExportAsync(
        ReadOnlyMemory<byte> metricData,
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
                new HttpOtlpSinkAuthSecretNames(
                    _options.BasicAuthSecretName,
                    _options.UsernameSecretName,
                    _options.PasswordSecretName)),
            metricData,
            contentType,
            new HttpOtlpSinkLogCallbacks(
                LogMetricsExported,
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
        Message = "Exported {ByteCount} bytes of metric data to Mimir")]
    private partial void LogMetricsExported(int byteCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to export metrics to Mimir. Status: {StatusCode}, Response: {Response}")]
    private partial void LogExportFailed(int statusCode, string response);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Retrying Mimir export (attempt {Attempt}/{MaxRetries}): {Error}")]
    private partial void LogExportRetry(int attempt, int maxRetries, string error);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Mimir endpoint not configured, skipping export")]
    private partial void LogEndpointNotConfigured();
}
