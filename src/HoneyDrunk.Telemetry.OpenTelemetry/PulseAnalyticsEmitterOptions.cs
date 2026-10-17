// <copyright file="PulseAnalyticsEmitterOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.OpenTelemetry;

/// <summary>
/// Configuration options for <see cref="PulseAnalyticsEmitter"/>.
/// </summary>
public sealed class PulseAnalyticsEmitterOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:Pulse:Analytics";

    /// <summary>
    /// The named HttpClient name used for analytics emission.
    /// </summary>
    public const string HttpClientName = "PulseAnalytics";

    /// <summary>
    /// Gets or sets the Pulse.Collector endpoint URL.
    /// </summary>
    public string CollectorEndpoint { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Gets or sets the service name to include in analytics requests.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
