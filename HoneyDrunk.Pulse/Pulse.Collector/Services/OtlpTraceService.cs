// <copyright file="OtlpTraceService.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using Google.Protobuf;
using Grpc.Core;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace HoneyDrunk.Pulse.Collector.Services;

/// <summary>
/// gRPC service for receiving OTLP traces.
/// </summary>
/// <remarks>
/// Implements the OpenTelemetry Collector Trace Service protocol.
/// Serializes incoming protobuf messages to bytes and delegates to the existing
/// ingestion pipeline for processing.
/// </remarks>
/// <param name="pipeline">The ingestion pipeline.</param>
/// <param name="parser">The OTLP parser.</param>
/// <param name="logger">The logger.</param>
public sealed class OtlpTraceService(
    Ingestion.IngestionPipeline pipeline,
    Ingestion.OtlpParser parser,
    ILogger<OtlpTraceService> logger) : TraceService.TraceServiceBase
{
    /// <inheritdoc/>
    public override async Task<ExportTraceServiceResponse> Export(
        ExportTraceServiceRequest request,
        ServerCallContext context)
    {
        try
        {
            // Serialize the protobuf request to bytes for forwarding to sinks
            var rawOtlpData = request.ToByteArray();

            // Parse directly from the protobuf request for better efficiency
            var result = parser.ParseTracesFromProto(request);

            // Extract source info from gRPC metadata
            var sourceName = context.RequestHeaders.GetValue("x-source-service")
                ?? (result.ResourceNames.Count > 0 ? result.ResourceNames[0] : null);
            var sourceNodeId = context.RequestHeaders.GetValue("x-source-nodeid");

            // Process through the pipeline with raw bytes for sink forwarding
            await pipeline.ProcessTracesAsync(
                result.SpanCount,
                sourceName,
                sourceNodeId,
                result.ErrorSpans,
                rawOtlpData: rawOtlpData,
                contentType: "application/x-protobuf",
                cancellationToken: context.CancellationToken).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "gRPC OTLP traces received: {SpanCount} spans ({ErrorCount} errors) from {Source}",
                    result.SpanCount,
                    result.ErrorSpans.Count,
                    sourceName ?? "unknown");
            }

            return new ExportTraceServiceResponse();
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // Client cancelled / timed out — surface as gRPC Cancelled rather than Internal so
            // server error rates aren't inflated by ordinary disconnects.
            throw new RpcException(new Status(StatusCode.Cancelled, "Trace export cancelled by client"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error processing gRPC OTLP traces");
            throw new RpcException(new Status(StatusCode.Internal, "Error processing traces"));
        }
    }
}
