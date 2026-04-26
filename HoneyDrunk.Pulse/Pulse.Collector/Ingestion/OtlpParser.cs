// <copyright file="OtlpParser.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using System.Globalization;
using System.Text.Json;

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Parses OTLP payloads for traces, metrics, and logs.
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides accurate parsing for both JSON and protobuf OTLP payloads.
/// </para>
/// <para>
/// <b>JSON parsing:</b> When content-type is application/json, the parser traverses
/// the OTLP JSON structure to count actual spans, metrics, or log records.
/// </para>
/// <para>
/// <b>Protobuf parsing:</b> When content-type is application/x-protobuf, the parser
/// uses generated OpenTelemetry proto types for accurate parsing and error extraction.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="OtlpParser"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
public sealed partial class OtlpParser(ILogger<OtlpParser> logger)
{
    /// <summary>
    /// OTLP severity number threshold for error-level logs.
    /// OTLP: 17-20 = ERROR, 21-24 = FATAL.
    /// </summary>
    private const int ErrorSeverityThreshold = 17;

    /// <summary>
    /// Parses an OTLP trace request from a stream.
    /// </summary>
    /// <param name="stream">The request body stream.</param>
    /// <param name="contentType">The content type header.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed result with span count and resource info.</returns>
    public async Task<OtlpTraceResult> ParseTracesAsync(
        Stream stream,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await ReadStreamAsync(stream, cancellationToken).ConfigureAwait(false);

            if (bytes.Length == 0)
            {
                return OtlpTraceResult.Empty;
            }

            // Parse span count and resource names
            var estimatedSpanCount = EstimateSpanCount(bytes, contentType);
            var resourceNames = ExtractResourceNames(bytes, contentType);

            // Extract error spans for forwarding to Sentry
            var errorSpans = ExtractErrorSpans(bytes, contentType);

            LogTracesParsed(estimatedSpanCount, errorSpans.Count, bytes.Length);

            return new OtlpTraceResult(estimatedSpanCount, resourceNames, errorSpans);
        }
        catch (Exception ex)
        {
            LogTraceParseError(ex);
            return OtlpTraceResult.Empty;
        }
    }

    /// <summary>
    /// Parses an OTLP trace request directly from a protobuf request object.
    /// Used by gRPC service to avoid re-serialization.
    /// </summary>
    /// <param name="request">The protobuf trace export request.</param>
    /// <returns>The parsed result with span count, resource info, and error spans.</returns>
    public OtlpTraceResult ParseTracesFromProto(ExportTraceServiceRequest request)
    {
        try
        {
            var spanCount = 0;
            var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errorSpans = new List<ExtractedErrorSpan>();

            foreach (var resourceSpan in request.ResourceSpans)
            {
                var serviceName = ExtractServiceNameFromProtoResource(resourceSpan.Resource);
                if (!string.IsNullOrEmpty(serviceName))
                {
                    resourceNames.Add(serviceName);
                }

                foreach (var scopeSpan in resourceSpan.ScopeSpans)
                {
                    foreach (var span in scopeSpan.Spans)
                    {
                        spanCount++;

                        var errorSpan = TryExtractErrorSpanFromProto(span, serviceName);
                        if (errorSpan != null)
                        {
                            errorSpans.Add(errorSpan);
                        }
                    }
                }
            }

            LogTracesParsed(spanCount, errorSpans.Count, 0);

            return new OtlpTraceResult(spanCount, [.. resourceNames], errorSpans);
        }
        catch (Exception ex)
        {
            LogTraceParseError(ex);
            return OtlpTraceResult.Empty;
        }
    }

    /// <summary>
    /// Parses an OTLP metrics request from a stream.
    /// </summary>
    /// <param name="stream">The request body stream.</param>
    /// <param name="contentType">The content type header.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed result with data point count and resource info.</returns>
    public async Task<OtlpMetricsResult> ParseMetricsAsync(
        Stream stream,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await ReadStreamAsync(stream, cancellationToken).ConfigureAwait(false);

            if (bytes.Length == 0)
            {
                return OtlpMetricsResult.Empty;
            }

            // Estimate counts based on payload characteristics
            var estimatedMetricCount = EstimateMetricCount(bytes, contentType);
            var estimatedDataPointCount = estimatedMetricCount; // 1:1 estimate for simplicity
            var resourceNames = ExtractResourceNames(bytes, contentType);

            LogMetricsParsed(estimatedMetricCount, bytes.Length);

            return new OtlpMetricsResult(estimatedMetricCount, estimatedDataPointCount, resourceNames);
        }
        catch (Exception ex)
        {
            LogMetricParseError(ex);
            return OtlpMetricsResult.Empty;
        }
    }

    /// <summary>
    /// Parses an OTLP logs request from a stream.
    /// </summary>
    /// <param name="stream">The request body stream.</param>
    /// <param name="contentType">The content type header.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed result with log record count and resource info.</returns>
    public async Task<OtlpLogsResult> ParseLogsAsync(
        Stream stream,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await ReadStreamAsync(stream, cancellationToken).ConfigureAwait(false);

            if (bytes.Length == 0)
            {
                return OtlpLogsResult.Empty;
            }

            // Parse log record count and resource names
            var estimatedLogCount = EstimateLogRecordCount(bytes, contentType);
            var resourceNames = ExtractResourceNames(bytes, contentType);

            // Extract error logs for forwarding to Sentry
            var errorLogs = ExtractErrorLogs(bytes, contentType);

            // Extract max severity for log level filtering
            var maxSeverity = ExtractMaxSeverity(bytes, contentType);

            LogLogsParsedWithErrors(estimatedLogCount, errorLogs.Count, bytes.Length);

            return new OtlpLogsResult(estimatedLogCount, resourceNames, errorLogs, maxSeverity);
        }
        catch (Exception ex)
        {
            LogLogParseError(ex);
            return OtlpLogsResult.Empty;
        }
    }

    /// <summary>
    /// Parses an OTLP logs request directly from a protobuf request object.
    /// Used by gRPC service to avoid re-serialization.
    /// </summary>
    /// <param name="request">The protobuf logs export request.</param>
    /// <returns>The parsed result with log count, resource info, and error logs.</returns>
    public OtlpLogsResult ParseLogsFromProto(ExportLogsServiceRequest request)
    {
        try
        {
            var logCount = 0;
            var maxSeverity = 0;
            var resourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errorLogs = new List<ExtractedErrorLog>();

            foreach (var resourceLog in request.ResourceLogs)
            {
                var serviceName = ExtractServiceNameFromProtoResource(resourceLog.Resource);
                if (!string.IsNullOrEmpty(serviceName))
                {
                    resourceNames.Add(serviceName);
                }

                foreach (var scopeLog in resourceLog.ScopeLogs)
                {
                    foreach (var logRecord in scopeLog.LogRecords)
                    {
                        logCount++;

                        // Track max severity for log level filtering
                        var severity = (int)logRecord.SeverityNumber;
                        if (severity > maxSeverity)
                        {
                            maxSeverity = severity;
                        }

                        var errorLog = TryExtractErrorLogFromProto(logRecord, serviceName);
                        if (errorLog != null)
                        {
                            errorLogs.Add(errorLog);
                        }
                    }
                }
            }

            LogLogsParsedWithErrors(logCount, errorLogs.Count, 0);

            return new OtlpLogsResult(logCount, [.. resourceNames], errorLogs, maxSeverity);
        }
        catch (Exception ex)
        {
            LogLogParseError(ex);
            return OtlpLogsResult.Empty;
        }
    }

    /// <summary>
    /// Tries to extract an error span if the span has error status or exception events.
    /// </summary>
    private static ExtractedErrorSpan? TryExtractErrorSpan(JsonElement span, string? serviceName)
    {
        // Check span status - OTLP status code 2 = ERROR
        var hasErrorStatus = false;
        string? statusMessage = null;

        if (span.TryGetProperty("status", out var status))
        {
            if (status.TryGetProperty("code", out var code))
            {
                // Status code: 0=UNSET, 1=OK, 2=ERROR
                hasErrorStatus = code.TryGetInt32(out var codeValue) && codeValue == 2;
            }

            if (status.TryGetProperty("message", out var message))
            {
                statusMessage = message.GetString();
            }
        }

        // Check for exception events
        string? exceptionType = null;
        string? exceptionMessage = null;
        string? stackTrace = null;

        if (span.TryGetProperty("events", out var events))
        {
            foreach (var evt in events.EnumerateArray())
            {
                if (evt.TryGetProperty("name", out var eventName) &&
                    eventName.GetString()?.Equals("exception", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (evt.TryGetProperty("attributes", out var eventAttrs))
                    {
                        foreach (var attr in eventAttrs.EnumerateArray())
                        {
                            var key = attr.TryGetProperty("key", out var k) ? k.GetString() : null;
                            var value = GetAttributeStringValue(attr);

                            switch (key)
                            {
                                case "exception.type":
                                    exceptionType = value;
                                    break;
                                case "exception.message":
                                    exceptionMessage = value;
                                    break;
                                case "exception.stacktrace":
                                    stackTrace = value;
                                    break;
                            }
                        }
                    }

                    // Found an exception event, mark as error
                    hasErrorStatus = true;
                    break;
                }
            }
        }

        if (!hasErrorStatus)
        {
            return null;
        }

        // Extract span details
        var spanName = span.TryGetProperty("name", out var name) ? name.GetString() : "unknown";
        var traceId = span.TryGetProperty("traceId", out var tid) ? tid.GetString() : null;
        var spanId = span.TryGetProperty("spanId", out var sid) ? sid.GetString() : null;

        // Extract additional attributes
        var attributes = new Dictionary<string, string>();
        if (span.TryGetProperty("attributes", out var spanAttrs))
        {
            foreach (var attr in spanAttrs.EnumerateArray())
            {
                var key = attr.TryGetProperty("key", out var k) ? k.GetString() : null;
                var value = GetAttributeStringValue(attr);

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    attributes[key] = value;
                }
            }
        }

        return new ExtractedErrorSpan(
            SpanName: spanName ?? "unknown",
            ServiceName: serviceName,
            ErrorMessage: statusMessage ?? exceptionMessage,
            ExceptionType: exceptionType,
            ExceptionMessage: exceptionMessage,
            StackTrace: stackTrace,
            TraceId: traceId,
            SpanId: spanId,
            Attributes: attributes);
    }

    /// <summary>
    /// Gets string value from an OTLP attribute.
    /// </summary>
    private static string? GetAttributeStringValue(JsonElement attr)
    {
        if (!attr.TryGetProperty("value", out var value))
        {
            return null;
        }

        if (value.TryGetProperty("stringValue", out var stringVal))
        {
            return stringVal.GetString();
        }

        if (value.TryGetProperty("intValue", out var intVal))
        {
            return intVal.ToString();
        }

        if (value.TryGetProperty("boolValue", out var boolVal))
        {
            return boolVal.GetBoolean().ToString();
        }

        return null;
    }

    /// <summary>
    /// Extracts service name from a resource span element.
    /// </summary>
    private static string? ExtractServiceNameFromResource(JsonElement resourceSpan)
    {
        if (!resourceSpan.TryGetProperty("resource", out var resource))
        {
            return null;
        }

        if (!resource.TryGetProperty("attributes", out var attributes))
        {
            return null;
        }

        foreach (var attr in attributes.EnumerateArray())
        {
            if (attr.TryGetProperty("key", out var key) &&
                key.GetString()?.Equals("service.name", StringComparison.OrdinalIgnoreCase) == true)
            {
                return GetAttributeStringValue(attr);
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts service.name from resource attributes in OTLP JSON.
    /// </summary>
    private static void ExtractServiceNamesFromResourceArray(
        JsonElement root,
        string arrayName,
        HashSet<string> names)
    {
        if (!root.TryGetProperty(arrayName, out var resourceArray))
        {
            return;
        }

        foreach (var resource in resourceArray.EnumerateArray())
        {
            if (!resource.TryGetProperty("resource", out var resourceElement))
            {
                continue;
            }

            if (!resourceElement.TryGetProperty("attributes", out var attributes))
            {
                continue;
            }

            foreach (var attr in attributes.EnumerateArray())
            {
                if (attr.TryGetProperty("key", out var key) &&
                    key.GetString()?.Equals("service.name", StringComparison.OrdinalIgnoreCase) == true &&
                    attr.TryGetProperty("value", out var value))
                {
                    // OTLP attribute values can be in different formats
                    if (value.TryGetProperty("stringValue", out var stringValue))
                    {
                        var serviceName = stringValue.GetString();
                        if (!string.IsNullOrWhiteSpace(serviceName))
                        {
                            names.Add(serviceName);
                        }
                    }
                }
            }
        }
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        return contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Extracts service name from a protobuf Resource.
    /// </summary>
    private static string? ExtractServiceNameFromProtoResource(OpenTelemetry.Proto.Resource.V1.Resource? resource)
    {
        if (resource == null)
        {
            return null;
        }

        foreach (var attr in resource.Attributes)
        {
            if (attr.Key.Equals("service.name", StringComparison.OrdinalIgnoreCase))
            {
                return attr.Value?.StringValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to extract an error span from a protobuf Span.
    /// </summary>
    private static ExtractedErrorSpan? TryExtractErrorSpanFromProto(Span span, string? serviceName)
    {
        // Check span status - Status code 2 = ERROR
        var hasErrorStatus = span.Status?.Code == StatusCode.Error;

        // Check for exception events
        string? exceptionType = null;
        string? exceptionMessage = null;
        string? stackTrace = null;

        foreach (var evt in span.Events)
        {
            if (evt.Name.Equals("exception", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var attr in evt.Attributes)
                {
                    switch (attr.Key)
                    {
                        case "exception.type":
                            exceptionType = GetProtoAttributeStringValue(attr.Value);
                            break;
                        case "exception.message":
                            exceptionMessage = GetProtoAttributeStringValue(attr.Value);
                            break;
                        case "exception.stacktrace":
                            stackTrace = GetProtoAttributeStringValue(attr.Value);
                            break;
                    }
                }

                hasErrorStatus = true;
                break;
            }
        }

        if (!hasErrorStatus)
        {
            return null;
        }

        // Extract span attributes
        var attributes = new Dictionary<string, string>();
        foreach (var attr in span.Attributes)
        {
            var value = GetProtoAttributeStringValue(attr.Value);
            if (!string.IsNullOrEmpty(value))
            {
                attributes[attr.Key] = value;
            }
        }

        return new ExtractedErrorSpan(
            SpanName: span.Name,
            ServiceName: serviceName,
            ErrorMessage: span.Status?.Message ?? exceptionMessage,
            ExceptionType: exceptionType,
            ExceptionMessage: exceptionMessage,
            StackTrace: stackTrace,
            TraceId: ConvertBytesToHex(span.TraceId),
            SpanId: ConvertBytesToHex(span.SpanId),
            Attributes: attributes);
    }

    /// <summary>
    /// Tries to extract an error log from a protobuf LogRecord.
    /// </summary>
    private static ExtractedErrorLog? TryExtractErrorLogFromProto(
        OpenTelemetry.Proto.Logs.V1.LogRecord logRecord,
        string? serviceName)
    {
        // Check if this is an error-level log (severityNumber >= 17 is ERROR, >= 21 is FATAL)
        var severityNumber = (int)logRecord.SeverityNumber;
        var isError = severityNumber >= ErrorSeverityThreshold;

        // Also check for exception attributes even if severity is lower
        string? exceptionType = null;
        string? exceptionMessage = null;
        string? stackTrace = null;

        foreach (var attr in logRecord.Attributes)
        {
            switch (attr.Key)
            {
                case "exception.type":
                    exceptionType = GetProtoAttributeStringValue(attr.Value);
                    isError = true; // Exception fields indicate an error
                    break;
                case "exception.message":
                    exceptionMessage = GetProtoAttributeStringValue(attr.Value);
                    isError = true;
                    break;
                case "exception.stacktrace":
                    stackTrace = GetProtoAttributeStringValue(attr.Value);
                    isError = true;
                    break;
            }
        }

        if (!isError)
        {
            return null;
        }

        // Extract log attributes
        var attributes = new Dictionary<string, string>();
        foreach (var attr in logRecord.Attributes)
        {
            var value = GetProtoAttributeStringValue(attr.Value);
            if (!string.IsNullOrEmpty(value))
            {
                attributes[attr.Key] = value;
            }
        }

        // Get message from body
        var message = GetProtoAnyValueString(logRecord.Body);

        // Get timestamp
        var timestamp = logRecord.TimeUnixNano > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)(logRecord.TimeUnixNano / 1_000_000))
            : DateTimeOffset.UtcNow;

        return new ExtractedErrorLog(
            Message: message ?? exceptionMessage,
            SeverityNumber: severityNumber,
            SeverityText: logRecord.SeverityText,
            Timestamp: timestamp,
            ServiceName: serviceName,
            TraceId: ConvertBytesToHex(logRecord.TraceId),
            SpanId: ConvertBytesToHex(logRecord.SpanId),
            ExceptionType: exceptionType,
            ExceptionMessage: exceptionMessage,
            StackTrace: stackTrace,
            Attributes: attributes);
    }

    /// <summary>
    /// Tries to extract an error log from JSON log record.
    /// </summary>
    private static ExtractedErrorLog? TryExtractErrorLogFromJson(JsonElement logRecord, string? serviceName)
    {
        // Get severity number
        var severityNumber = 0;
        if (logRecord.TryGetProperty("severityNumber", out var sevNum))
        {
            severityNumber = sevNum.TryGetInt32(out var sn) ? sn : 0;
        }

        var isError = severityNumber >= ErrorSeverityThreshold;

        // Get severity text
        string? severityText = null;
        if (logRecord.TryGetProperty("severityText", out var sevText))
        {
            severityText = sevText.GetString();

            // Also check severity text for error indicators
            if (!isError && severityText != null)
            {
                isError = severityText.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                          severityText.Equals("FATAL", StringComparison.OrdinalIgnoreCase) ||
                          severityText.Equals("CRITICAL", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Check for exception attributes
        string? exceptionType = null;
        string? exceptionMessage = null;
        string? stackTrace = null;
        var attributes = new Dictionary<string, string>();

        if (logRecord.TryGetProperty("attributes", out var attrs))
        {
            foreach (var attr in attrs.EnumerateArray())
            {
                var key = attr.TryGetProperty("key", out var k) ? k.GetString() : null;
                var value = GetAttributeStringValue(attr);

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                attributes[key] = value;

                switch (key)
                {
                    case "exception.type":
                        exceptionType = value;
                        isError = true;
                        break;
                    case "exception.message":
                        exceptionMessage = value;
                        isError = true;
                        break;
                    case "exception.stacktrace":
                        stackTrace = value;
                        isError = true;
                        break;
                }
            }
        }

        if (!isError)
        {
            return null;
        }

        // Get message from body
        string? message = null;
        if (logRecord.TryGetProperty("body", out var body))
        {
            if (body.TryGetProperty("stringValue", out var strVal))
            {
                message = strVal.GetString();
            }
        }

        // Get timestamp
        var timestamp = DateTimeOffset.UtcNow;
        if (logRecord.TryGetProperty("timeUnixNano", out var timeNano))
        {
            if (timeNano.TryGetUInt64(out var nanos))
            {
                timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(nanos / 1_000_000));
            }
        }

        // Get trace/span IDs
        var traceId = logRecord.TryGetProperty("traceId", out var tid) ? tid.GetString() : null;
        var spanId = logRecord.TryGetProperty("spanId", out var sid) ? sid.GetString() : null;

        return new ExtractedErrorLog(
            Message: message ?? exceptionMessage,
            SeverityNumber: severityNumber,
            SeverityText: severityText,
            Timestamp: timestamp,
            ServiceName: serviceName,
            TraceId: traceId,
            SpanId: spanId,
            ExceptionType: exceptionType,
            ExceptionMessage: exceptionMessage,
            StackTrace: stackTrace,
            Attributes: attributes);
    }

    /// <summary>
    /// Gets string value from protobuf AnyValue.
    /// </summary>
    private static string? GetProtoAttributeStringValue(AnyValue? value)
    {
        if (value == null)
        {
            return null;
        }

        return value.ValueCase switch
        {
            AnyValue.ValueOneofCase.StringValue => value.StringValue,
            AnyValue.ValueOneofCase.IntValue => value.IntValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.DoubleValue => value.DoubleValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.BoolValue => value.BoolValue.ToString(),
            AnyValue.ValueOneofCase.None => value.StringValue,
            AnyValue.ValueOneofCase.ArrayValue => value.StringValue,
            AnyValue.ValueOneofCase.KvlistValue => value.StringValue,
            AnyValue.ValueOneofCase.BytesValue => value.StringValue,
            _ => value.StringValue,
        };
    }

    /// <summary>
    /// Gets string value from protobuf AnyValue (string-only extraction).
    /// </summary>
    private static string? GetProtoAnyValueString(AnyValue? value)
    {
        if (value == null)
        {
            return null;
        }

        return value.ValueCase switch
        {
            AnyValue.ValueOneofCase.StringValue => value.StringValue,
            AnyValue.ValueOneofCase.None => null,
            AnyValue.ValueOneofCase.BoolValue => null,
            AnyValue.ValueOneofCase.IntValue => null,
            AnyValue.ValueOneofCase.DoubleValue => null,
            AnyValue.ValueOneofCase.ArrayValue => null,
            AnyValue.ValueOneofCase.KvlistValue => null,
            AnyValue.ValueOneofCase.BytesValue => null,
            _ => null,
        };
    }

    /// <summary>
    /// Converts byte array to hex string.
    /// </summary>
    private static string? ConvertBytesToHex(Google.Protobuf.ByteString? bytes)
    {
        if (bytes == null || bytes.IsEmpty)
        {
            return null;
        }

        return Convert.ToHexString(bytes.ToByteArray()).ToLowerInvariant();
    }

    private int EstimateSpanCount(byte[] bytes, string? contentType)
    {
        // For JSON, parse and count actual spans
        if (IsJsonContentType(contentType))
        {
            return CountOtlpSpansFromJson(bytes);
        }

        // Protobuf heuristic: average span is ~300 bytes
        return Math.Max(1, bytes.Length / 300);
    }

    private int EstimateMetricCount(byte[] bytes, string? contentType)
    {
        if (IsJsonContentType(contentType))
        {
            return CountOtlpMetricsFromJson(bytes);
        }

        // Protobuf heuristic: average metric is ~100 bytes
        return Math.Max(1, bytes.Length / 100);
    }

    private int EstimateLogRecordCount(byte[] bytes, string? contentType)
    {
        if (IsJsonContentType(contentType))
        {
            return CountOtlpLogRecordsFromJson(bytes);
        }

        // Protobuf heuristic: average log record is ~150 bytes
        return Math.Max(1, bytes.Length / 150);
    }

    /// <summary>
    /// Counts spans from OTLP JSON trace data.
    /// OTLP structure: { resourceSpans: [{ scopeSpans: [{ spans: [...] }] }] }.
    /// </summary>
    private int CountOtlpSpansFromJson(byte[] bytes)
    {
        try
        {
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;
            var spanCount = 0;

            // Navigate: resourceSpans -> scopeSpans -> spans
            if (root.TryGetProperty("resourceSpans", out var resourceSpans))
            {
                foreach (var resourceSpan in resourceSpans.EnumerateArray())
                {
                    if (resourceSpan.TryGetProperty("scopeSpans", out var scopeSpans))
                    {
                        foreach (var scopeSpan in scopeSpans.EnumerateArray())
                        {
                            if (scopeSpan.TryGetProperty("spans", out var spans))
                            {
                                spanCount += spans.GetArrayLength();
                            }
                        }
                    }
                }
            }

            return Math.Max(1, spanCount);
        }
        catch (Exception ex)
        {
            LogJsonSpanParseFallback(ex);
            return Math.Max(1, bytes.Length / 300);
        }
    }

    /// <summary>
    /// Counts metrics from OTLP JSON metrics data.
    /// OTLP structure: { resourceMetrics: [{ scopeMetrics: [{ metrics: [...] }] }] }.
    /// </summary>
    private int CountOtlpMetricsFromJson(byte[] bytes)
    {
        try
        {
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;
            var metricCount = 0;

            // Navigate: resourceMetrics -> scopeMetrics -> metrics
            if (root.TryGetProperty("resourceMetrics", out var resourceMetrics))
            {
                foreach (var resourceMetric in resourceMetrics.EnumerateArray())
                {
                    if (resourceMetric.TryGetProperty("scopeMetrics", out var scopeMetrics))
                    {
                        foreach (var scopeMetric in scopeMetrics.EnumerateArray())
                        {
                            if (scopeMetric.TryGetProperty("metrics", out var metrics))
                            {
                                metricCount += metrics.GetArrayLength();
                            }
                        }
                    }
                }
            }

            return Math.Max(1, metricCount);
        }
        catch (Exception ex)
        {
            LogJsonMetricParseFallback(ex);
            return Math.Max(1, bytes.Length / 100);
        }
    }

    /// <summary>
    /// Counts log records from OTLP JSON logs data.
    /// OTLP structure: { resourceLogs: [{ scopeLogs: [{ logRecords: [...] }] }] }.
    /// </summary>
    private int CountOtlpLogRecordsFromJson(byte[] bytes)
    {
        try
        {
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;
            var logCount = 0;

            // Navigate: resourceLogs -> scopeLogs -> logRecords
            if (root.TryGetProperty("resourceLogs", out var resourceLogs))
            {
                foreach (var resourceLog in resourceLogs.EnumerateArray())
                {
                    if (resourceLog.TryGetProperty("scopeLogs", out var scopeLogs))
                    {
                        foreach (var scopeLog in scopeLogs.EnumerateArray())
                        {
                            if (scopeLog.TryGetProperty("logRecords", out var logRecords))
                            {
                                logCount += logRecords.GetArrayLength();
                            }
                        }
                    }
                }
            }

            return Math.Max(1, logCount);
        }
        catch (Exception ex)
        {
            LogJsonLogParseFallback(ex);
            return Math.Max(1, bytes.Length / 150);
        }
    }

    /// <summary>
    /// Extracts resource service names from OTLP JSON data.
    /// </summary>
    private List<string> ExtractResourceNames(byte[] bytes, string? contentType)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!IsJsonContentType(contentType))
        {
            // For protobuf, use proto types for parsing
            try
            {
                // Try trace format first
                var traceRequest = ExportTraceServiceRequest.Parser.ParseFrom(bytes);
                foreach (var rs in traceRequest.ResourceSpans)
                {
                    var serviceName = ExtractServiceNameFromProtoResource(rs.Resource);
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        names.Add(serviceName);
                    }
                }

                if (names.Count > 0)
                {
                    return [.. names];
                }
            }
            catch
            {
                // Not a trace request, try logs
            }

            try
            {
                var logsRequest = ExportLogsServiceRequest.Parser.ParseFrom(bytes);
                foreach (var rl in logsRequest.ResourceLogs)
                {
                    var serviceName = ExtractServiceNameFromProtoResource(rl.Resource);
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        names.Add(serviceName);
                    }
                }
            }
            catch
            {
                // Not a logs request either, return empty
            }

            return [.. names];
        }

        try
        {
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;

            // Try all three OTLP resource array types
            ExtractServiceNamesFromResourceArray(root, "resourceSpans", names);
            ExtractServiceNamesFromResourceArray(root, "resourceMetrics", names);
            ExtractServiceNamesFromResourceArray(root, "resourceLogs", names);
        }
        catch (Exception ex)
        {
            LogServiceNameExtractionFailed(ex);
        }

        return [.. names];
    }

    /// <summary>
    /// Extracts error spans from OTLP trace data for forwarding to error sinks.
    /// Supports both JSON and protobuf content types.
    /// </summary>
    private List<ExtractedErrorSpan> ExtractErrorSpans(byte[] bytes, string? contentType)
    {
        var errorSpans = new List<ExtractedErrorSpan>();

        if (!IsJsonContentType(contentType))
        {
            // Parse protobuf and extract errors
            return ExtractErrorSpansFromProtobuf(bytes);
        }

        try
        {
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;

            if (!root.TryGetProperty("resourceSpans", out var resourceSpans))
            {
                return errorSpans;
            }

            foreach (var resourceSpan in resourceSpans.EnumerateArray())
            {
                var serviceName = ExtractServiceNameFromResource(resourceSpan);

                if (!resourceSpan.TryGetProperty("scopeSpans", out var scopeSpans))
                {
                    continue;
                }

                foreach (var scopeSpan in scopeSpans.EnumerateArray())
                {
                    if (!scopeSpan.TryGetProperty("spans", out var spans))
                    {
                        continue;
                    }

                    foreach (var span in spans.EnumerateArray())
                    {
                        var errorSpan = TryExtractErrorSpan(span, serviceName);
                        if (errorSpan != null)
                        {
                            errorSpans.Add(errorSpan);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogErrorSpanExtractionFailed(ex);
        }

        return errorSpans;
    }

    /// <summary>
    /// Extracts error spans from protobuf OTLP trace data.
    /// </summary>
    private List<ExtractedErrorSpan> ExtractErrorSpansFromProtobuf(byte[] bytes)
    {
        var errorSpans = new List<ExtractedErrorSpan>();

        try
        {
            var request = ExportTraceServiceRequest.Parser.ParseFrom(bytes);

            foreach (var resourceSpan in request.ResourceSpans)
            {
                var serviceName = ExtractServiceNameFromProtoResource(resourceSpan.Resource);

                foreach (var scopeSpan in resourceSpan.ScopeSpans)
                {
                    foreach (var span in scopeSpan.Spans)
                    {
                        var errorSpan = TryExtractErrorSpanFromProto(span, serviceName);
                        if (errorSpan != null)
                        {
                            errorSpans.Add(errorSpan);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogProtobufTraceParseError(ex);
        }

        return errorSpans;
    }

    /// <summary>
    /// Extracts error logs from OTLP log data for forwarding to error sinks.
    /// Supports both JSON and protobuf content types.
    /// </summary>
    private List<ExtractedErrorLog> ExtractErrorLogs(byte[] bytes, string? contentType)
    {
        var errorLogs = new List<ExtractedErrorLog>();

        if (!IsJsonContentType(contentType))
        {
            // Parse protobuf and extract error logs
            return ExtractErrorLogsFromProtobuf(bytes);
        }

        try
        {
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;

            if (!root.TryGetProperty("resourceLogs", out var resourceLogs))
            {
                return errorLogs;
            }

            foreach (var resourceLog in resourceLogs.EnumerateArray())
            {
                var serviceName = ExtractServiceNameFromResource(resourceLog);

                if (!resourceLog.TryGetProperty("scopeLogs", out var scopeLogs))
                {
                    continue;
                }

                foreach (var scopeLog in scopeLogs.EnumerateArray())
                {
                    if (!scopeLog.TryGetProperty("logRecords", out var logRecords))
                    {
                        continue;
                    }

                    foreach (var logRecord in logRecords.EnumerateArray())
                    {
                        var errorLog = TryExtractErrorLogFromJson(logRecord, serviceName);
                        if (errorLog != null)
                        {
                            errorLogs.Add(errorLog);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogErrorLogExtractionFailed(ex);
        }

        return errorLogs;
    }

    /// <summary>
    /// Extracts the maximum severity number from OTLP log data for log level filtering.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1204:Static elements should appear before instance elements",
        Justification = "Helper kept adjacent to OTLP log parsing helpers for readability.")]
    private static int ExtractMaxSeverity(byte[] bytes, string? contentType)
    {
        var maxSeverity = 0;

        if (!IsJsonContentType(contentType))
        {
            return ExtractMaxSeverityFromProtobuf(bytes);
        }

        try
        {
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;

            if (!root.TryGetProperty("resourceLogs", out var resourceLogs))
            {
                return maxSeverity;
            }

            foreach (var resourceLog in resourceLogs.EnumerateArray())
            {
                if (!resourceLog.TryGetProperty("scopeLogs", out var scopeLogs))
                {
                    continue;
                }

                foreach (var scopeLog in scopeLogs.EnumerateArray())
                {
                    if (!scopeLog.TryGetProperty("logRecords", out var logRecords))
                    {
                        continue;
                    }

                    foreach (var logRecord in logRecords.EnumerateArray())
                    {
                        if (logRecord.TryGetProperty("severityNumber", out var sevNum) &&
                            sevNum.TryGetInt32(out var severity) &&
                            severity > maxSeverity)
                        {
                            maxSeverity = severity;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parsing errors for severity extraction
        }

        return maxSeverity;
    }

    /// <summary>
    /// Extracts the maximum severity number from protobuf OTLP log data.
    /// </summary>
    private static int ExtractMaxSeverityFromProtobuf(byte[] bytes)
    {
        var maxSeverity = 0;

        try
        {
            var request = ExportLogsServiceRequest.Parser.ParseFrom(bytes);

            foreach (var resourceLog in request.ResourceLogs)
            {
                foreach (var scopeLog in resourceLog.ScopeLogs)
                {
                    foreach (var logRecord in scopeLog.LogRecords)
                    {
                        var severity = (int)logRecord.SeverityNumber;
                        if (severity > maxSeverity)
                        {
                            maxSeverity = severity;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parsing errors for severity extraction
        }

        return maxSeverity;
    }

    /// <summary>
    /// Extracts error logs from protobuf OTLP log data.
    /// </summary>
    private List<ExtractedErrorLog> ExtractErrorLogsFromProtobuf(byte[] bytes)
    {
        var errorLogs = new List<ExtractedErrorLog>();

        try
        {
            var request = ExportLogsServiceRequest.Parser.ParseFrom(bytes);

            foreach (var resourceLog in request.ResourceLogs)
            {
                var serviceName = ExtractServiceNameFromProtoResource(resourceLog.Resource);

                foreach (var scopeLog in resourceLog.ScopeLogs)
                {
                    foreach (var logRecord in scopeLog.LogRecords)
                    {
                        var errorLog = TryExtractErrorLogFromProto(logRecord, serviceName);
                        if (errorLog != null)
                        {
                            errorLogs.Add(errorLog);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogProtobufLogParseError(ex);
        }

        return errorLogs;
    }
}
