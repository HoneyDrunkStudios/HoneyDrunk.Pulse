// <copyright file="OtlpEndpoints.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Pulse.Collector.Ingestion;
using HoneyDrunk.Telemetry.Abstractions.Models;
using System.Text.Json;

namespace HoneyDrunk.Pulse.Collector.Endpoints;

/// <summary>
/// OTLP receiving endpoint configuration.
/// </summary>
public static class OtlpEndpoints
{
    /// <summary>
    /// Maps OTLP receiving endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapOtlpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // OTLP HTTP Traces endpoint
        endpoints.MapPost("/otlp/v1/traces", HandleTracesAsync)
            .WithName("OtlpTraces")
            .WithTags("OTLP")
            .Accepts<object>("application/x-protobuf", "application/json");

        // OTLP HTTP Metrics endpoint
        endpoints.MapPost("/otlp/v1/metrics", HandleMetricsAsync)
            .WithName("OtlpMetrics")
            .WithTags("OTLP")
            .Accepts<object>("application/x-protobuf", "application/json");

        // OTLP HTTP Logs endpoint
        endpoints.MapPost("/otlp/v1/logs", HandleLogsAsync)
            .WithName("OtlpLogs")
            .WithTags("OTLP")
            .Accepts<object>("application/x-protobuf", "application/json");

        // Custom analytics events endpoint
        endpoints.MapPost("/otlp/v1/analytics", HandleAnalyticsAsync)
            .WithName("Analytics")
            .WithTags("Analytics")
            .Accepts<AnalyticsEventsRequest>("application/json");

        // Error reporting endpoint
        endpoints.MapPost("/otlp/v1/errors", HandleErrorAsync)
            .WithName("Errors")
            .WithTags("Errors")
            .Accepts<ErrorReportRequest>("application/json");

        return endpoints;
    }

    private static async Task<IResult> HandleTracesAsync(
        HttpContext context,
        IngestionPipeline pipeline,
        OtlpParser parser,
        ILogger<Program> logger)
    {
        try
        {
            // Buffer the request body for both parsing and forwarding to sinks
            using var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream, context.RequestAborted).ConfigureAwait(false);
            var rawOtlpData = memoryStream.ToArray();
            memoryStream.Position = 0;

            // Parse the OTLP request to extract actual counts
            var contentType = context.Request.ContentType;
            var result = await parser.ParseTracesAsync(
                memoryStream,
                contentType,
                context.RequestAborted).ConfigureAwait(false);

            var sourceName = context.Request.Headers["X-Source-Service"].FirstOrDefault()
                ?? (result.ResourceNames.Count > 0 ? result.ResourceNames[0] : null);
            var sourceNodeId = context.Request.Headers["X-Source-NodeId"].FirstOrDefault();

            // Pass error spans for forwarding to Sentry and raw OTLP data for trace sinks
            await pipeline.ProcessTracesAsync(
                result.SpanCount,
                sourceName,
                sourceNodeId,
                result.ErrorSpans,
                rawOtlpData: rawOtlpData,
                contentType: contentType,
                cancellationToken: context.RequestAborted).ConfigureAwait(false);

            return Results.Ok(new
            {
                Status = "accepted",
                result.SpanCount,
                ErrorCount = result.ErrorSpans.Count,
            });
        }
        catch (Exception ex)
        {
            logger.LogTracesRequestError(ex);
            return Results.Problem("Error processing traces", statusCode: 500);
        }
    }

    private static async Task<IResult> HandleMetricsAsync(
        HttpContext context,
        IngestionPipeline pipeline,
        OtlpParser parser,
        ILogger<Program> logger)
    {
        try
        {
            // Buffer the request body for both parsing and forwarding to sinks
            using var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream, context.RequestAborted).ConfigureAwait(false);
            var rawOtlpData = memoryStream.ToArray();
            memoryStream.Position = 0;

            var contentType = context.Request.ContentType;
            var result = await parser.ParseMetricsAsync(
                memoryStream,
                contentType,
                context.RequestAborted).ConfigureAwait(false);

            var sourceName = context.Request.Headers["X-Source-Service"].FirstOrDefault()
                ?? (result.ResourceNames.Count > 0 ? result.ResourceNames[0] : null);
            var sourceNodeId = context.Request.Headers["X-Source-NodeId"].FirstOrDefault();

            await pipeline.ProcessMetricsAsync(
                result.MetricCount,
                sourceName,
                sourceNodeId,
                rawOtlpData: rawOtlpData,
                contentType: contentType,
                cancellationToken: context.RequestAborted).ConfigureAwait(false);

            return Results.Ok(new { Status = "accepted", result.MetricCount, result.DataPointCount });
        }
        catch (Exception ex)
        {
            logger.LogMetricsRequestError(ex);
            return Results.Problem("Error processing metrics", statusCode: 500);
        }
    }

    private static async Task<IResult> HandleLogsAsync(
        HttpContext context,
        IngestionPipeline pipeline,
        OtlpParser parser,
        ILogger<Program> logger)
    {
        try
        {
            // Buffer the request body for both parsing and forwarding to sinks
            using var memoryStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memoryStream, context.RequestAborted).ConfigureAwait(false);
            var rawOtlpData = memoryStream.ToArray();
            memoryStream.Position = 0;

            var contentType = context.Request.ContentType;
            var result = await parser.ParseLogsAsync(
                memoryStream,
                contentType,
                context.RequestAborted).ConfigureAwait(false);

            var sourceName = context.Request.Headers["X-Source-Service"].FirstOrDefault()
                ?? (result.ResourceNames.Count > 0 ? result.ResourceNames[0] : null);
            var sourceNodeId = context.Request.Headers["X-Source-NodeId"].FirstOrDefault();

            // Pass error logs for forwarding to Sentry and raw OTLP data for log sinks
            await pipeline.ProcessLogsAsync(
                result.LogRecordCount,
                sourceName,
                sourceNodeId,
                result.ErrorLogs,
                rawOtlpData: rawOtlpData,
                contentType: contentType,
                maxSeverityNumber: result.MaxSeverityNumber,
                cancellationToken: context.RequestAborted).ConfigureAwait(false);

            return Results.Ok(new
            {
                Status = "accepted",
                result.LogRecordCount,
                ErrorLogCount = result.ErrorLogs.Count,
            });
        }
        catch (Exception ex)
        {
            logger.LogLogsRequestError(ex);
            return Results.Problem("Error processing logs", statusCode: 500);
        }
    }

    private static async Task<IResult> HandleAnalyticsAsync(
        HttpContext context,
        IngestionPipeline pipeline,
        ILogger<Program> logger)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync<AnalyticsEventsRequest>(context.RequestAborted)
                .ConfigureAwait(false);

            if (request?.Events is null || request.Events.Count == 0)
            {
                return Results.BadRequest(new { Error = "No events provided" });
            }

            var events = request.Events.Select(e =>
            {
                var telemetryEvent = new TelemetryEvent
                {
                    EventName = e.EventName,
                    Timestamp = e.Timestamp ?? DateTimeOffset.UtcNow,
                    DistinctId = e.DistinctId,
                    UserId = e.UserId,
                    SessionId = e.SessionId,
                    CorrelationId = e.CorrelationId,
                    NodeId = e.NodeId,
                    Environment = e.Environment,
                };

                if (e.Properties is not null)
                {
                    foreach (var prop in e.Properties)
                    {
                        telemetryEvent.Properties[prop.Key] = prop.Value;
                    }
                }

                return telemetryEvent;
            }).ToList();

            var sourceName = request.SourceService
                ?? context.Request.Headers["X-Source-Service"].FirstOrDefault();
            var sourceNodeId = request.SourceNodeId
                ?? context.Request.Headers["X-Source-NodeId"].FirstOrDefault();

            await pipeline.ProcessAnalyticsEventsAsync(
                events,
                sourceName,
                sourceNodeId,
                context.RequestAborted).ConfigureAwait(false);

            return Results.Ok(new { Status = "accepted", events.Count });
        }
        catch (JsonException ex)
        {
            logger.LogAnalyticsInvalidJson(ex);
            return Results.BadRequest(new { Error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            logger.LogAnalyticsRequestError(ex);
            return Results.Problem("Error processing analytics events", statusCode: 500);
        }
    }

    private static async Task<IResult> HandleErrorAsync(
        HttpContext context,
        IngestionPipeline pipeline,
        ILogger<Program> logger)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync<ErrorReportRequest>(context.RequestAborted)
                .ConfigureAwait(false);

            if (request is null)
            {
                return Results.BadRequest(new { Error = "Invalid request" });
            }

            var errorEvent = new ErrorEvent
            {
                Message = request.Message,
                Severity = request.Severity ?? TelemetryEventSeverity.Error,
                CorrelationId = request.CorrelationId,
                OperationId = request.OperationId,
                NodeId = request.NodeId,
                UserId = request.UserId,
                Environment = request.Environment,
            };

            if (request.Tags is not null)
            {
                foreach (var tag in request.Tags)
                {
                    errorEvent.Tags[tag.Key] = tag.Value;
                }
            }

            if (request.Extra is not null)
            {
                foreach (var extra in request.Extra)
                {
                    errorEvent.Extra[extra.Key] = extra.Value;
                }
            }

            var sourceName = context.Request.Headers["X-Source-Service"].FirstOrDefault();
            await pipeline.ProcessErrorAsync(errorEvent, sourceName, context.RequestAborted).ConfigureAwait(false);

            return Results.Ok(new { Status = "accepted" });
        }
        catch (JsonException ex)
        {
            logger.LogErrorReportInvalidJson(ex);
            return Results.BadRequest(new { Error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            logger.LogErrorReportRequestError(ex);
            return Results.Problem("Error processing error report", statusCode: 500);
        }
    }
}
