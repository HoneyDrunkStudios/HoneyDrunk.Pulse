// <copyright file="OtlpPayloadRedactor.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using Google.Protobuf;
using Google.Protobuf.Reflection;
using HoneyDrunk.Telemetry.OpenTelemetry.Redaction;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HoneyDrunk.Pulse.Collector.Ingestion;

/// <summary>
/// Sanitizes an OTLP batch once before fan-out, using the shared redaction policy.
/// Malformed, unsupported, or excessively nested payloads never fall back to raw export.
/// </summary>
public static class OtlpPayloadRedactor
{
    private const int MaxDepth = 32;

    /// <summary>Sanitizes an OTLP traces request.</summary>
    /// <param name="data">The incoming payload.</param>
    /// <param name="contentType">The incoming content type.</param>
    /// <returns>A sanitized payload in the same encoding.</returns>
    public static ReadOnlyMemory<byte> RedactTraces(ReadOnlyMemory<byte> data, string contentType)
        => Redact(data, contentType, ExportTraceServiceRequest.Parser);

    /// <summary>Sanitizes an OTLP metrics request.</summary>
    /// <param name="data">The incoming payload.</param>
    /// <param name="contentType">The incoming content type.</param>
    /// <returns>A sanitized payload in the same encoding.</returns>
    public static ReadOnlyMemory<byte> RedactMetrics(ReadOnlyMemory<byte> data, string contentType)
        => Redact(data, contentType, ExportMetricsServiceRequest.Parser);

    /// <summary>Sanitizes an OTLP logs request.</summary>
    /// <param name="data">The incoming payload.</param>
    /// <param name="contentType">The incoming content type.</param>
    /// <returns>A sanitized payload in the same encoding.</returns>
    public static ReadOnlyMemory<byte> RedactLogs(ReadOnlyMemory<byte> data, string contentType)
        => Redact(data, contentType, ExportLogsServiceRequest.Parser);

    private static ReadOnlyMemory<byte> Redact(ReadOnlyMemory<byte> data, string contentType, MessageParser parser)
    {
        var mediaType = contentType.Split(';', 2)[0].Trim();
        try
        {
            if (string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                var json = JsonNode.Parse(data.Span, documentOptions: new JsonDocumentOptions { MaxDepth = MaxDepth });
                if (json is not JsonObject)
                {
                    throw new InvalidDataException("An OTLP JSON request must be an object.");
                }

                RedactJson(json, 0);
                return JsonSerializer.SerializeToUtf8Bytes(json);
            }

            if (!string.Equals(mediaType, "application/x-protobuf", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Unsupported OTLP content type.");
            }

            // Unknown protobuf fields cannot be inspected by this schema. Drop them rather
            // than retaining an opaque path around the redaction boundary.
            var message = parser.WithDiscardUnknownFields(true).ParseFrom(data.Span);
            RedactMessage(message, 0);
            return message.ToByteArray();
        }
        catch (Exception ex) when (ex is JsonException or InvalidProtocolBufferException)
        {
            // Do not attach parser exceptions: their text may contain rejected input.
            throw new InvalidDataException("The OTLP payload could not be safely sanitized.");
        }
    }

    private static void RedactMessage(IMessage message, int depth)
    {
        ValidateDepth(depth);
        if (message is KeyValue attribute && TelemetryRedactor.IsSensitiveKey(attribute.Key))
        {
            // Keys are producer-controlled text too; the fast path must sanitize both sides.
            attribute.Key = TelemetryRedactor.RedactText(attribute.Key) ?? string.Empty;
            attribute.Value = new AnyValue { StringValue = TelemetryRedactor.RedactedValue };
            return;
        }

        if (message is AnyValue { ValueCase: AnyValue.ValueOneofCase.BytesValue } value)
        {
            // Binary attribute/body values cannot be inspected as text.
            value.StringValue = TelemetryRedactor.RedactedValue;
            return;
        }

        foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
        {
            var fieldValue = field.Accessor.GetValue(message);
            if (field.IsRepeated && fieldValue is IEnumerable values)
            {
                foreach (var item in values.OfType<IMessage>())
                {
                    RedactMessage(item, depth + 1);
                }
            }
            else if (fieldValue is IMessage child)
            {
                RedactMessage(child, depth + 1);
            }
            else if (field.FieldType == FieldType.String && fieldValue is string { Length: > 0 } text)
            {
                // Empty oneof alternatives must stay unset.
                field.Accessor.SetValue(message, TelemetryRedactor.RedactText(text));
            }
        }
    }

    private static void RedactJson(JsonNode node, int depth)
    {
        ValidateDepth(depth);
        if (node is JsonArray array)
        {
            RedactJsonArray(array, depth);
        }
        else if (node is JsonObject obj)
        {
            RedactJsonObject(obj, depth);
        }
    }

    private static void RedactJsonArray(JsonArray array, int depth)
    {
        for (var index = 0; index < array.Count; index++)
        {
            if (array[index] is JsonValue value && value.TryGetValue<string>(out var text))
            {
                array[index] = TelemetryRedactor.RedactText(text);
            }
            else if (array[index] is { } child)
            {
                RedactJson(child, depth + 1);
            }
        }
    }

    private static void RedactJsonObject(JsonObject obj, int depth)
    {
        if (obj["key"] is JsonValue key && key.TryGetValue<string>(out var attributeKey)
            && TelemetryRedactor.IsSensitiveKey(attributeKey))
        {
            obj["value"] = new JsonObject { ["stringValue"] = TelemetryRedactor.RedactedValue };
        }

        if (obj.Remove("bytesValue"))
        {
            obj["stringValue"] = TelemetryRedactor.RedactedValue;
        }

        foreach (var property in obj.ToArray())
        {
            if (TelemetryRedactor.IsSensitiveKey(property.Key))
            {
                obj[property.Key] = TelemetryRedactor.RedactedValue;
            }
            else if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
            {
                obj[property.Key] = TelemetryRedactor.RedactText(text);
            }
            else if (property.Value is { } child)
            {
                RedactJson(child, depth + 1);
            }
        }
    }

    private static void ValidateDepth(int depth)
    {
        if (depth > MaxDepth)
        {
            throw new InvalidDataException("The OTLP payload exceeds the safe nesting limit.");
        }
    }
}
