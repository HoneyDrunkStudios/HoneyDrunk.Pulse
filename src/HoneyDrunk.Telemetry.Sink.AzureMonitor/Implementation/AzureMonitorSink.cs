// <copyright file="AzureMonitorSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.AzureMonitor.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Telemetry.Sink.AzureMonitor.Implementation;

/// <summary>
/// Receipt-only Azure Monitor sink. Implements <see cref="ITraceSink"/>, <see cref="ILogSink"/>,
/// and <see cref="IMetricsSink"/> as a pass-through observer that records that telemetry data was
/// received and that the Azure Monitor connection string is configured.
/// </summary>
/// <remarks>
/// <para>
/// <b>This sink does not perform export by itself.</b> Actual delivery to Application Insights /
/// Azure Monitor happens via the OpenTelemetry SDK exporter pipeline (e.g. the Azure Monitor
/// OpenTelemetry exporter package), registered separately in the host. The sink exists to:
/// </para>
/// <list type="bullet">
///   <item>Validate that an Azure Monitor connection string is present in Vault when the AzureMonitor sink is enabled (Invariant 9).</item>
///   <item>Emit a debug log per signal type so receipt is observable for end-to-end smoke tests.</item>
///   <item>Provide a uniform <see cref="ITraceSink"/>/<see cref="ILogSink"/>/<see cref="IMetricsSink"/> registration shape consistent with the other sinks (Loki, Mimir, Tempo, Sentry, PostHog).</item>
/// </list>
/// <para>
/// If full HTTP-forwarding behavior (matching Loki/Mimir/Tempo) is ever required for Azure Monitor,
/// this class becomes the place to add it. Until then, treat it as a configuration-only adapter.
/// </para>
/// </remarks>
/// <param name="options">The Azure Monitor sink options.</param>
/// <param name="connectionString">The Azure Monitor connection string from Vault. May be null when the sink is disabled or in development.</param>
/// <param name="logger">The logger.</param>
public sealed partial class AzureMonitorSink(
    IOptions<AzureMonitorSinkOptions> options,
    string? connectionString,
    ILogger<AzureMonitorSink> logger) : ITraceSink, ILogSink, IMetricsSink, IDisposable
{
    private readonly AzureMonitorSinkOptions _options = options.Value;
    private bool _disposed;

    /// <inheritdoc />
    async Task ITraceSink.ExportAsync(
        ReadOnlyMemory<byte> traceData,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.ExportTraces)
        {
            return;
        }

        await ExportInternalAsync(traceData, contentType, "traces", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task ILogSink.ExportAsync(
        ReadOnlyMemory<byte> logData,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.ExportLogs)
        {
            return;
        }

        await ExportInternalAsync(logData, contentType, "logs", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    async Task IMetricsSink.ExportAsync(
        ReadOnlyMemory<byte> metricData,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_options.ExportMetrics)
        {
            return;
        }

        await ExportInternalAsync(metricData, contentType, "metrics", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    Task ITraceSink.FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    Task ILogSink.FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    Task IMetricsSink.FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    private async Task ExportInternalAsync(
        ReadOnlyMemory<byte> data,
        string contentType,
        string signalType,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(connectionString))
        {
            LogConnectionStringNotConfigured(signalType);
            return;
        }

        try
        {
            // Azure Monitor SDK handles the actual export through the configured TracerProvider/MeterProvider
            // For the Collector forwarding scenario, we log that we received the data
            // The actual export happens through the OpenTelemetry SDK exporter pipeline
            LogDataReceived(signalType, contentType, data.Length);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogExportFailed(signalType, ex.Message);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Received {SignalType} data ({ContentType}, {ByteCount} bytes) for Azure Monitor export")]
    private partial void LogDataReceived(string signalType, string contentType, int byteCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to export {SignalType} to Azure Monitor: {Error}")]
    private partial void LogExportFailed(string signalType, string error);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Azure Monitor connection string not configured, skipping {SignalType} export")]
    private partial void LogConnectionStringNotConfigured(string signalType);
}
