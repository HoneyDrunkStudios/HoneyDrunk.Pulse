// <copyright file="OtlpLogsService.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using Google.Protobuf;
using Grpc.Core;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace HoneyDrunk.Pulse.Collector.Services;

/// <summary>
/// gRPC service for receiving OTLP logs.
/// </summary>
/// <remarks>
/// Implements the OpenTelemetry Collector Logs Service protocol.
/// Serializes incoming protobuf messages to bytes and delegates to the existing
/// ingestion pipeline for processing.
/// </remarks>
/// <param name="pipeline">The ingestion pipeline.</param>
/// <param name="parser">The OTLP parser.</param>
/// <param name="logger">The logger.</param>
public sealed class OtlpLogsService(
    Ingestion.IngestionPipeline pipeline,
    Ingestion.OtlpParser parser,
    ILogger<OtlpLogsService> logger) : LogsService.LogsServiceBase
{
    /// <inheritdoc/>
    public override async Task<ExportLogsServiceResponse> Export(
        ExportLogsServiceRequest request,
        ServerCallContext context)
    {
        try
        {
            // Serialize the protobuf request to bytes for forwarding to sinks
            var rawOtlpData = request.ToByteArray();

            // Parse directly from the protobuf request for better efficiency
            var result = parser.ParseLogsFromProto(request);

            // Extract source info from gRPC metadata
            var sourceName = context.RequestHeaders.GetValue("x-source-service")
                ?? (result.ResourceNames.Count > 0 ? result.ResourceNames[0] : null);
            var sourceNodeId = context.RequestHeaders.GetValue("x-source-nodeid");

            // Process through the pipeline with error logs and raw bytes for sink forwarding
            await pipeline.ProcessLogsAsync(
                result.LogRecordCount,
                sourceName,
                sourceNodeId,
                result.ErrorLogs,
                rawOtlpData: rawOtlpData,
                contentType: "application/x-protobuf",
                maxSeverityNumber: result.MaxSeverityNumber,
                cancellationToken: context.CancellationToken).ConfigureAwait(false);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "gRPC OTLP logs received: {LogCount} logs ({ErrorCount} errors) from {Source}",
                    result.LogRecordCount,
                    result.ErrorLogs.Count,
                    sourceName ?? "unknown");
            }

            return new ExportLogsServiceResponse();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing gRPC OTLP logs");
            throw new RpcException(new Status(StatusCode.Internal, "Error processing logs"));
        }
    }
}
