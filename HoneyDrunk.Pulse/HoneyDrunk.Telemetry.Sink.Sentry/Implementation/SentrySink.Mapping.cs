// <copyright file="SentrySink.Mapping.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Models;
using Sentry.Protocol;

namespace HoneyDrunk.Telemetry.Sink.Sentry.Implementation;

/// <summary>Maps sanitized exception metadata to native Sentry exception events.</summary>
public sealed partial class SentrySink
{
    internal static SentryEvent? CreateExceptionEvent(ErrorEvent errorEvent)
    {
        if (errorEvent.Exception is not null)
        {
            // Direct sink callers retain the SDK's existing CaptureException behavior.
            return null;
        }

        errorEvent.Tags.TryGetValue("exception.type", out var exceptionType);
        if (string.IsNullOrWhiteSpace(exceptionType))
        {
            errorEvent.Extra.TryGetValue("exception.type", out var extraType);
            exceptionType = extraType as string;
        }

        errorEvent.Extra.TryGetValue("exception.message", out var extraMessage);
        errorEvent.Extra.TryGetValue("exception.stacktrace", out var extraStackTrace);
        var exceptionMessage = extraMessage as string;
        var stackTrace = extraStackTrace as string;
        var structuredStackTrace = CreateStructuredStackTrace(errorEvent);
        if (string.IsNullOrWhiteSpace(exceptionType)
            && string.IsNullOrWhiteSpace(exceptionMessage)
            && string.IsNullOrWhiteSpace(stackTrace)
            && structuredStackTrace is null)
        {
            return null;
        }

        // Collector redaction intentionally omits raw Exception.Data and inner exceptions.
        // Use only its sanitized fields, preserving native exception grouping without
        // rebuilding an Exception that the SDK could inspect for additional private data.
        var sentryEvent = new SentryEvent
        {
            Message = errorEvent.Message,
            Level = MapSeverityToSentryLevel(errorEvent.Severity),
            SentryExceptions =
            [
                new SentryException
                {
                    Type = exceptionType,
                    Value = exceptionMessage ?? errorEvent.Message,
                    Stacktrace = structuredStackTrace,
                },
            ],
        };
        if (!string.IsNullOrWhiteSpace(stackTrace))
        {
            // Text-only OTLP errors have no structured .NET frames. Preserve their
            // sanitized trace for diagnosis without inventing stack frames.
            sentryEvent.SetExtra("exception.stacktrace", stackTrace);
        }

        return sentryEvent;
    }

    private static SentryStackTrace? CreateStructuredStackTrace(ErrorEvent errorEvent)
    {
        if (!errorEvent.Extra.TryGetValue("exception.stacktrace.frames", out var value)
            || value is not IEnumerable<object?> frameValues)
        {
            return null;
        }

        var frames = new List<SentryStackFrame>();
        foreach (var frameValue in frameValues.Take(128))
        {
            if (frameValue is not IReadOnlyDictionary<string, object?> fields)
            {
                continue;
            }

            fields.TryGetValue("function", out var function);
            fields.TryGetValue("filename", out var fileName);
            fields.TryGetValue("lineno", out var lineNumber);
            fields.TryGetValue("colno", out var columnNumber);
            if (function is not string && fileName is not string)
            {
                continue;
            }

            // Redaction already emits oldest-first frames containing only safe scalars.
            frames.Add(new SentryStackFrame
            {
                Function = function as string,
                FileName = fileName as string,
                LineNumber = lineNumber is int line && line > 0 ? line : null,
                ColumnNumber = columnNumber is int column && column > 0 ? column : null,
            });
        }

        return frames.Count == 0 ? null : new SentryStackTrace { Frames = frames };
    }
}
