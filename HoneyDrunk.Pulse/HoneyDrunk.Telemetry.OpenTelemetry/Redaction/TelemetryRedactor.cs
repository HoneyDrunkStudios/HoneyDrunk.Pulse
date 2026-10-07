// <copyright file="TelemetryRedactor.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Models;
using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HoneyDrunk.Telemetry.OpenTelemetry.Redaction;

/// <summary>
/// Applies bounded, conservative redaction to known credential and personal-data fields.
/// </summary>
/// <remarks>
/// This is defense in depth, not a guarantee that arbitrary unlabeled secrets can be recognized.
/// Unsupported objects, excessive nesting, and oversized values are replaced rather than serialized.
/// </remarks>
public static partial class TelemetryRedactor
{
    /// <summary>
    /// The replacement used for sensitive or unsafe-to-inspect values.
    /// </summary>
    public const string RedactedValue = "[REDACTED]";

    private const int MaximumTextLength = 16384;
    private const int MaximumDepth = 8;
    private const int MaximumValues = 1024;
    private const int MaximumExceptionFrames = 128;
    private const string SensitiveFieldPattern = @"(?:password|passwd|pwd|(?:access[_. -]?|refresh[_. -]?|id[_. -]?)?token|api[_. -]?key|authorization|cookie|(?:client[_. -]?)?secret|connection[_. -]?string|e[_. -]?mail(?:[_. -]?address)?|phone(?:[_. -]?number)?)";
    private const RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking;

    private static readonly string[] SensitiveFieldMarkers =
    [
        "password", "passwd", "pwd", "token", "apikey", "authorization", "cookie",
        "secret", "connectionstring", "email", "phone",
    ];

    /// <summary>
    /// Determines whether a field name indicates credentials or personal contact information.
    /// </summary>
    /// <param name="key">The field name, matched without punctuation or case distinctions.</param>
    /// <returns>Whether the entire field value should be replaced.</returns>
    public static bool IsSensitiveKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length > 256)
        {
            return true;
        }

        var normalized = new string([.. key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
        return SensitiveFieldMarkers.Any(marker => normalized.Contains(marker, StringComparison.Ordinal));
    }

    /// <summary>
    /// Removes common labeled credentials, authorization values, URL credentials, and email addresses.
    /// </summary>
    /// <param name="value">The text to inspect.</param>
    /// <returns>Sanitized text, or null for a null input.</returns>
    public static string? RedactText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length > MaximumTextLength)
        {
            return RedactedValue;
        }

        try
        {
            var redacted = WholeFieldPattern().Replace(value, "${key}" + RedactedValue);
            redacted = CredentialPattern().Replace(redacted, "${key}" + RedactedValue);
            redacted = AuthorizationPattern().Replace(redacted, RedactedValue);
            redacted = UrlCredentialPattern().Replace(redacted, "${scheme}" + RedactedValue + "@");
            return EmailPattern().Replace(redacted, RedactedValue);
        }
        catch (RegexMatchTimeoutException)
        {
            // Telemetry must never fall back to exporting the original text after a timeout.
            return RedactedValue;
        }
    }

    /// <summary>
    /// Copies and sanitizes a field, recursively inspecting dictionaries, lists, and JSON values.
    /// </summary>
    /// <param name="key">The field name.</param>
    /// <param name="value">The field value.</param>
    /// <returns>A sanitized value without modifying caller-owned collections.</returns>
    public static object? RedactValue(string key, object? value)
        => RedactValue(key, value, out _);

    /// <summary>
    /// Copies an analytics event and sanitizes its text and properties.
    /// </summary>
    /// <param name="telemetryEvent">The caller-owned event.</param>
    /// <returns>A sanitized event with correlation identifiers in their original fields.</returns>
    public static TelemetryEvent RedactTelemetryEvent(TelemetryEvent telemetryEvent)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        var result = new TelemetryEvent
        {
            EventName = RedactText(telemetryEvent.EventName) ?? string.Empty,
            Timestamp = telemetryEvent.Timestamp,
            DistinctId = RedactText(telemetryEvent.DistinctId),
            UserId = RedactText(telemetryEvent.UserId),
            SessionId = RedactText(telemetryEvent.SessionId),
            CorrelationId = RedactText(telemetryEvent.CorrelationId),
            OperationId = RedactText(telemetryEvent.OperationId),
            NodeId = RedactText(telemetryEvent.NodeId),
            NodeName = RedactText(telemetryEvent.NodeName),
            GridId = RedactText(telemetryEvent.GridId),
            TenantId = RedactText(telemetryEvent.TenantId),
            Environment = RedactText(telemetryEvent.Environment),
        };
        CopyProperties(telemetryEvent.Properties, result.Properties);
        return result;
    }

    /// <summary>
    /// Copies an error without retaining the raw exception object, its data, or inner exceptions.
    /// </summary>
    /// <param name="errorEvent">The caller-owned error.</param>
    /// <returns>A sanitized error with exception details in standard named fields.</returns>
    public static ErrorEvent RedactErrorEvent(ErrorEvent errorEvent)
    {
        ArgumentNullException.ThrowIfNull(errorEvent);
        var result = new ErrorEvent
        {
            Message = RedactText(errorEvent.Message ?? errorEvent.Exception?.Message),
            Severity = errorEvent.Severity,
            Timestamp = errorEvent.Timestamp,
            CorrelationId = RedactText(errorEvent.CorrelationId),
            OperationId = RedactText(errorEvent.OperationId),
            NodeId = RedactText(errorEvent.NodeId),
            UserId = RedactText(errorEvent.UserId),
            Environment = RedactText(errorEvent.Environment),
            Release = RedactText(errorEvent.Release),
        };
        foreach (var tag in errorEvent.Tags)
        {
            result.Tags[RedactText(tag.Key) ?? string.Empty] = IsSensitiveKey(tag.Key)
                ? RedactedValue : RedactText(tag.Value) ?? string.Empty;
        }

        CopyProperties(errorEvent.Extra, result.Extra);
        if (errorEvent.Exception is { } exception)
        {
            result.Tags["exception.type"] = RedactText(exception.GetType().FullName) ?? string.Empty;
            result.Extra["exception.message"] = RedactText(exception.Message);
            result.Extra["exception.stacktrace"] = RedactText(exception.StackTrace);
            result.Extra["exception.stacktrace.frames"] = GetExceptionFrames(exception);
        }

        return result;
    }

    internal static object? RedactValue(string key, object? value, out bool redacted)
    {
        var remaining = MaximumValues;
        redacted = false;
        return RedactValue(key, value, 0, ref remaining, ref redacted);
    }

    [GeneratedRegex(@"(?<key>\b(?:authorization|proxy[_. -]?authorization|(?:set[_. -]?)?cookie|connection[_. -]?string|phone(?:[_. -]?number)?)[""']?\s*[:=]\s*)(?:\[REDACTED\]|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|[^\r\n]+)", PatternOptions, 100)]
    private static partial Regex WholeFieldPattern();

    [GeneratedRegex(@"(?<key>\b" + SensitiveFieldPattern + @"[""']?\s*[:=]\s*)(?:\[REDACTED\]|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|[^\s,;\]\}&]+)", PatternOptions, 100)]
    private static partial Regex CredentialPattern();

    [GeneratedRegex(@"\b(?:Bearer|Basic)\s+[a-z0-9._~+/=\-]+", PatternOptions, 100)]
    private static partial Regex AuthorizationPattern();

    [GeneratedRegex(@"(?<scheme>[a-z][a-z0-9+.\-]*://)[^/\s:@]+:[^/\s@]+@", PatternOptions, 100)]
    private static partial Regex UrlCredentialPattern();

    [GeneratedRegex(@"[a-z0-9.!#$%&'*+/=?^_`{|}~\-]+@[a-z0-9\-]+(?:\.[a-z0-9\-]+)+", PatternOptions, 100)]
    private static partial Regex EmailPattern();

    private static List<Dictionary<string, object?>> GetExceptionFrames(Exception exception)
    {
        var result = new List<Dictionary<string, object?>>();
        var frames = new StackTrace(exception, true).GetFrames();

        // Retain the frames nearest the throw, ordered oldest-first for native error backends.
        foreach (var frame in frames.Take(MaximumExceptionFrames).Reverse())
        {
            var metadata = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (frame.GetMethod() is { } method)
            {
                var function = method.DeclaringType?.FullName is { } declaringType
                    ? $"{declaringType}.{method.Name}" : method.Name;
                metadata["function"] = RedactText(function);
            }

            if (frame.GetFileName() is { } filename)
            {
                metadata["filename"] = RedactText(filename);
            }

            if (frame.GetFileLineNumber() is > 0 and var lineNumber)
            {
                metadata["lineno"] = lineNumber;
            }

            if (frame.GetFileColumnNumber() is > 0 and var columnNumber)
            {
                metadata["colno"] = columnNumber;
            }

            result.Add(metadata);
        }

        return result;
    }

    private static void CopyProperties(Dictionary<string, object?> source, Dictionary<string, object?> destination)
    {
        var remaining = MaximumValues;
        var redacted = false;
        foreach (var property in source.Take(MaximumValues))
        {
            destination[RedactText(property.Key, ref redacted) ?? string.Empty] = RedactValue(property.Key, property.Value, 0, ref remaining, ref redacted);
        }
    }

    private static object? RedactValue(string key, object? value, int depth, ref int remaining, ref bool redacted)
    {
        if (IsSensitiveKey(key) || depth > MaximumDepth || --remaining < 0)
        {
            return ReplaceValue(ref redacted);
        }

        switch (value)
        {
            case null:
                return null;
            case string text:
                return RedactText(text, ref redacted);
            case JsonElement json:
                return RedactJson(json, depth, ref remaining, ref redacted);
            case IEnumerable<KeyValuePair<string, object?>> properties:
                var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in properties)
                {
                    if (remaining <= 0)
                    {
                        return ReplaceValue(ref redacted);
                    }

                    fields[RedactText(property.Key, ref redacted) ?? string.Empty] = RedactValue(property.Key, property.Value, depth + 1, ref remaining, ref redacted);
                }

                return fields;
            case IDictionary dictionary:
                var entries = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (remaining <= 0 || entry.Key is not string entryKey)
                    {
                        return ReplaceValue(ref redacted);
                    }

                    entries[RedactText(entryKey, ref redacted) ?? string.Empty] = RedactValue(entryKey, entry.Value, depth + 1, ref remaining, ref redacted);
                }

                return entries;
            case Array array when array.GetType().GetElementType() is { } elementType
                && (elementType == typeof(bool) || elementType == typeof(int) || elementType == typeof(long)
                    || elementType == typeof(float) || elementType == typeof(double)):
                if (array.Length > remaining)
                {
                    return ReplaceValue(ref redacted);
                }

                remaining -= array.Length;
                return array.Clone();
            case IEnumerable sequence:
                var items = new List<object?>();
                foreach (var item in sequence)
                {
                    if (remaining <= 0)
                    {
                        return ReplaceValue(ref redacted);
                    }

                    items.Add(RedactValue(string.Empty, item, depth + 1, ref remaining, ref redacted));
                }

                // Preserve homogeneous string arrays for standard OTel attribute exporters.
                return value is string[] ? items.Cast<string?>().ToArray() : items.ToArray();
            case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
            case DateTime or DateTimeOffset or TimeSpan or Guid:
                return value;
            default:
                // Do not call ToString or serialize arbitrary objects that may contain private fields.
                return ReplaceValue(ref redacted);
        }
    }

    private static object? RedactJson(JsonElement value, int depth, ref int remaining, ref bool redacted)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (remaining <= 0)
                    {
                        return ReplaceValue(ref redacted);
                    }

                    fields[RedactText(property.Name, ref redacted) ?? string.Empty] = RedactValue(property.Name, property.Value, depth + 1, ref remaining, ref redacted);
                }

                return fields;
            case JsonValueKind.Array:
                var items = new List<object?>();
                foreach (var item in value.EnumerateArray())
                {
                    if (remaining <= 0)
                    {
                        return ReplaceValue(ref redacted);
                    }

                    items.Add(RedactValue(string.Empty, item, depth + 1, ref remaining, ref redacted));
                }

                return items.ToArray();
            case JsonValueKind.String:
                return RedactText(value.GetString(), ref redacted);
            case JsonValueKind.Number:
                return value.Clone();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null;
        }
    }

    private static string? RedactText(string? value, ref bool redacted)
    {
        var result = RedactText(value);
        redacted |= result != value;
        return result;
    }

    private static string ReplaceValue(ref bool redacted)
    {
        redacted = true;
        return RedactedValue;
    }
}
