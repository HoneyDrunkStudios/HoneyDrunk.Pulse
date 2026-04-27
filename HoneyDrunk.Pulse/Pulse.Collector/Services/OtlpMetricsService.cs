// <copyright file="OtlpMetricsService.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using Google.Protobuf;
using Grpc.Core;
using OpenTelemetry.Proto.Collector.Metrics.V1;

namespace HoneyDrunk.Pulse.Collector.Services;

/// <summary>
/// gRPC service for receiving OTLP metrics.
/// </summary>
/// <remarks>
/// Implements the OpenTelemetry Collector Metrics Service protocol.
/// Serializes incoming protobuf messages to bytes and delegates to the existing
/// ingestion pipeline for processing.
/// </remarks>
/// <param name="pipeline">The ingestion pipeline.</param>
/// <param name="parser">The OTLP parser.</param>
/// <param name="logger">The logger.</param>
public sealed class OtlpMetricsService(
    Ingestion.IngestionPipeline pipeline,
    Ingestion.OtlpParser parser,
    ILogger<OtlpMetricsService> logger) : MetricsService.MetricsServiceBase
{
    /// <inheritdoc/>
    public override async Task<ExportMetricsServiceResponse> Export(
        ExportMetricsServiceRequest request,
        ServerCallContext context)
    {
        try
        {
            // Serialize the request to bytes for forwarding to sinks
            var rawOtlpData = request.ToByteArray();
            using var memoryStream = new MemoryStream(rawOtlpData);

            // Parse using existing infrastructure
            var result = await parser.ParseMetricsAsync(
                memoryStream,
                "application/x-protobuf",
                context.CancellationToken).ConfigureAwait(false);

            // Extract source info from gRPC metadata
            var sourceName = context.RequestHeaders.GetValue("x-source-service")
                ?? (result.ResourceNames.Count > 0 ? result.ResourceNames[0] : null);
            var sourceNodeId = context.RequestHeaders.GetValue("x-source-nodeid");

            // Process through the pipeline with raw bytes for sink forwarding
            await pipeline.ProcessMetricsAsync(
                result.MetricCount,
                sourceName,
                sourceNodeId,
                rawOtlpData: rawOtlpData,
                contentType: "application/x-protobuf",
                cancellationToken: context.CancellationToken).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "gRPC OTLP metrics received: {MetricCount} metrics from {Source}",
                    result.MetricCount,
                    sourceName ?? "unknown");
            }

            return new ExportMetricsServiceResponse();
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // Client cancelled / timed out — surface as gRPC Cancelled rather than Internal so
            // server error rates aren't inflated by ordinary disconnects.
            throw new RpcException(new Status(StatusCode.Cancelled, "Metrics export cancelled by client"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error processing gRPC OTLP metrics");
            throw new RpcException(new Status(StatusCode.Internal, "Error processing metrics"));
        }
    }
}
