// <copyright file="TelemetryNames.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Abstractions.Conventions;

/// <summary>
/// Provides standard naming conventions for telemetry sources across HoneyDrunk services.
/// </summary>
public static class TelemetryNames
{
    /// <summary>
    /// The prefix used for all HoneyDrunk telemetry sources.
    /// </summary>
    public const string Prefix = "HoneyDrunk";

    /// <summary>
    /// Gets the standard ActivitySource name for a given service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The formatted ActivitySource name.</returns>
    public static string GetActivitySourceName(string serviceName)
        => $"{Prefix}.{serviceName}";

    /// <summary>
    /// Gets the standard Meter name for a given service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The formatted Meter name.</returns>
    public static string GetMeterName(string serviceName)
        => $"{Prefix}.{serviceName}.Metrics";

    /// <summary>
    /// Gets the standard event name for analytics events.
    /// </summary>
    /// <param name="category">The event category.</param>
    /// <param name="action">The event action.</param>
    /// <returns>The formatted event name.</returns>
    public static string GetEventName(string category, string action)
        => $"{Prefix}.{category}.{action}";

    /// <summary>
    /// Standard activity source names for core HoneyDrunk components.
    /// </summary>
    public static class ActivitySources
    {
        /// <summary>
        /// ActivitySource name for the Pulse collector.
        /// </summary>
        public const string PulseCollector = $"{Prefix}.Pulse.Collector";

        /// <summary>
        /// ActivitySource name for Kernel operations.
        /// </summary>
        public const string Kernel = $"{Prefix}.Kernel";

        /// <summary>
        /// ActivitySource name for Transport operations.
        /// </summary>
        public const string Transport = $"{Prefix}.Transport";

        /// <summary>
        /// ActivitySource name for Vault operations.
        /// </summary>
        public const string Vault = $"{Prefix}.Vault";
    }

    /// <summary>
    /// Standard meter names for core HoneyDrunk components.
    /// </summary>
    public static class Meters
    {
        /// <summary>
        /// Meter name for the Pulse collector.
        /// </summary>
        public const string PulseCollector = $"{Prefix}.Pulse.Collector.Metrics";

        /// <summary>
        /// Meter name for Kernel operations.
        /// </summary>
        public const string Kernel = $"{Prefix}.Kernel.Metrics";
    }

    /// <summary>
    /// Standard event categories for analytics.
    /// </summary>
    public static class EventCategories
    {
        /// <summary>
        /// Events related to user interactions.
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// Events related to features.
        /// </summary>
        public const string Feature = "Feature";

        /// <summary>
        /// Events related to system operations.
        /// </summary>
        public const string System = "System";

        /// <summary>
        /// Events related to business/creator metrics.
        /// </summary>
        public const string Business = "Business";
    }
}
