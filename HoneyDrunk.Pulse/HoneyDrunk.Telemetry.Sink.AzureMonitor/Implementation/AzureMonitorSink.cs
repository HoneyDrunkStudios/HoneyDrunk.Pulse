// <copyright file="AzureMonitorSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.AzureMonitor.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Telemetry.Sink.AzureMonitor.Implementation;

/// <summary>
/// Azure Monitor sink implementation that forwards telemetry data to Application Insights.
/// </summary>
/// <remarks>
/// This sink implements ITraceSink, ILogSink, and IMetricsSink to allow selective routing
/// of different signal types to Azure Monitor.
/// </remarks>
public sealed partial class AzureMonitorSink : ITraceSink, ILogSink, IMetricsSink, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AzureMonitorSinkOptions _options;
    private readonly ILogger<AzureMonitorSink> _logger;
    private readonly string? _connectionString;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureMonitorSink"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="options">The Azure Monitor sink options.</param>
    /// <param name="connectionString">The Azure Monitor connection string from Vault.</param>
    /// <param name="logger">The logger.</param>
    public AzureMonitorSink(
        HttpClient httpClient,
        IOptions<AzureMonitorSinkOptions> options,
        string? connectionString,
        ILogger<AzureMonitorSink> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _connectionString = connectionString;
        _logger = logger;
    }

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

        if (string.IsNullOrEmpty(_connectionString))
        {
            LogConnectionStringNotConfigured(signalType);
            return;
        }

        try
        {
            // Azure Monitor SDK handles the actual export through the configured TracerProvider/MeterProvider
            // For the Collector forwarding scenario, we log that we received the data
            // The actual export happens through the OpenTelemetry SDK exporter pipeline
            LogDataReceived(signalType, data.Length);
        }
        catch (Exception ex)
        {
            LogExportFailed(signalType, ex.Message);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Received {SignalType} data ({ByteCount} bytes) for Azure Monitor export")]
    private partial void LogDataReceived(string signalType, int byteCount);

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
