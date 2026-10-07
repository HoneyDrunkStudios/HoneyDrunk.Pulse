// <copyright file="LogRedactionProcessor.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using OpenTelemetry;
using OpenTelemetry.Logs;

namespace HoneyDrunk.Telemetry.OpenTelemetry.Redaction;

/// <summary>
/// Sanitizes log attributes, message text, and exception details before export.
/// </summary>
/// <remarks>
/// Register before exporters. Scopes cannot be replaced through the SDK's public API;
/// disable scope collection when using this processor. Resource attributes are not modified.
/// </remarks>
public sealed class LogRedactionProcessor : BaseProcessor<LogRecord>
{
    /// <inheritdoc/>
    public override void OnEnd(LogRecord data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var attributes = new List<KeyValuePair<string, object?>>();
        var suppressFormattedMessage = false;
        if (data.Attributes is { } originalAttributes)
        {
            foreach (var attribute in originalAttributes)
            {
                var sanitized = TelemetryRedactor.RedactValue(attribute.Key, attribute.Value, out var redacted);
                attributes.Add(new(TelemetryRedactor.RedactText(attribute.Key) ?? string.Empty, sanitized));
                if (attribute.Key != "{OriginalFormat}" && redacted)
                {
                    suppressFormattedMessage = true;
                }
            }
        }

        // Formatting has already happened. A sensitive structured value may be interpolated
        // without its field label, so text-pattern matching alone cannot sanitize that copy.
        var bodyIsFormattedMessage = data.Body == data.FormattedMessage;
        data.FormattedMessage = suppressFormattedMessage
            ? TelemetryRedactor.RedactedValue : TelemetryRedactor.RedactText(data.FormattedMessage);
        data.Body = suppressFormattedMessage && bodyIsFormattedMessage
            ? TelemetryRedactor.RedactedValue : TelemetryRedactor.RedactText(data.Body);

        if (data.Exception is { } exception)
        {
            attributes.RemoveAll(attribute => attribute.Key is "exception.type" or "exception.message" or "exception.stacktrace");
            attributes.Add(new("exception.type", TelemetryRedactor.RedactText(exception.GetType().FullName)));
            attributes.Add(new("exception.message", TelemetryRedactor.RedactText(exception.Message)));
            attributes.Add(new("exception.stacktrace", TelemetryRedactor.RedactText(exception.StackTrace)));
            data.Exception = null;
        }

        data.Attributes = attributes;
    }
}
