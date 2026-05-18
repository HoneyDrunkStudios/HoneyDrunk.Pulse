// <copyright file="HttpOtlpSinkLogCallbacks.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Shared;

/// <summary>
/// Sink-specific logging callbacks used by the shared HTTP OTLP exporter.
/// </summary>
internal sealed class HttpOtlpSinkLogCallbacks
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpOtlpSinkLogCallbacks"/> class.
    /// </summary>
    /// <param name="exported">Called when an export succeeds.</param>
    /// <param name="exportFailed">Called when an export receives an unsuccessful response.</param>
    /// <param name="exportRetry">Called before a retry after a transient HTTP failure.</param>
    /// <param name="endpointNotConfigured">Called when the sink endpoint is missing.</param>
    public HttpOtlpSinkLogCallbacks(
        Action<int> exported,
        Action<int, string> exportFailed,
        Action<int, int, string> exportRetry,
        Action endpointNotConfigured)
    {
        Exported = exported;
        ExportFailed = exportFailed;
        ExportRetry = exportRetry;
        EndpointNotConfigured = endpointNotConfigured;
    }

    /// <summary>
    /// Gets the success callback.
    /// </summary>
    public Action<int> Exported { get; }

    /// <summary>
    /// Gets the unsuccessful-response callback.
    /// </summary>
    public Action<int, string> ExportFailed { get; }

    /// <summary>
    /// Gets the retry callback.
    /// </summary>
    public Action<int, int, string> ExportRetry { get; }

    /// <summary>
    /// Gets the missing-endpoint callback.
    /// </summary>
    public Action EndpointNotConfigured { get; }
}
