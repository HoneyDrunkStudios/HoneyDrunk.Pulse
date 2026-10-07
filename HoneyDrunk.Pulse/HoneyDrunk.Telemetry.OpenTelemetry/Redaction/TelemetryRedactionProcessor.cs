// <copyright file="TelemetryRedactionProcessor.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using OpenTelemetry;
using System.Diagnostics;

namespace HoneyDrunk.Telemetry.OpenTelemetry.Redaction;

/// <summary>
/// Sanitizes exportable activity data before downstream OpenTelemetry processors run.
/// </summary>
/// <remarks>
/// Register before exporters. Resource attributes and propagated baggage are outside this processor's scope.
/// </remarks>
public sealed class TelemetryRedactionProcessor : BaseProcessor<Activity>
{
    /// <inheritdoc/>
    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);
        data.DisplayName = TelemetryRedactor.RedactText(data.DisplayName) ?? string.Empty;
        data.SetStatus(data.Status, TelemetryRedactor.RedactText(data.StatusDescription));

        // The public ref enumerator also replaces duplicate tags, unlike SetTag which updates only the first.
        foreach (ref var tag in data.EnumerateTagObjects())
        {
            tag = RedactTag(tag);
        }

        foreach (ref var activityEvent in data.EnumerateEvents())
        {
            activityEvent = new ActivityEvent(
                TelemetryRedactor.RedactText(activityEvent.Name) ?? string.Empty,
                activityEvent.Timestamp,
                new ActivityTagsCollection(activityEvent.Tags.Select(RedactTag)));
        }

        foreach (ref var link in data.EnumerateLinks())
        {
            link = new ActivityLink(link.Context, new ActivityTagsCollection(link.Tags?.Select(RedactTag) ?? []));
        }
    }

    private static KeyValuePair<string, object?> RedactTag(KeyValuePair<string, object?> tag)
        => new(TelemetryRedactor.RedactText(tag.Key) ?? string.Empty, TelemetryRedactor.RedactValue(tag.Key, tag.Value));
}
