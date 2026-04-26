// <copyright file="SentrySinkOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Sentry.Options;

/// <summary>
/// Configuration options for the Sentry error tracking sink.
/// </summary>
public sealed class SentrySinkOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:Sentry";

    /// <summary>
    /// Gets or sets the Vault secret name for the Sentry DSN (Data Source Name).
    /// </summary>
    public string DsnSecretName { get; set; } = "Sentry--Dsn";

    /// <summary>
    /// Gets or sets a value indicating whether the sink is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the environment name (e.g., production, staging).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the release/version identifier.
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// Gets or sets the sample rate for error events (0.0 to 1.0).
    /// </summary>
    public double SampleRate { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the sample rate for traces (0.0 to 1.0).
    /// </summary>
    public double TracesSampleRate { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the maximum number of breadcrumbs to record.
    /// </summary>
    public int MaxBreadcrumbs { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to attach stack traces to messages.
    /// </summary>
    public bool AttachStacktrace { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to send default PII.
    /// </summary>
    public bool SendDefaultPii { get; set; }

    /// <summary>
    /// Gets additional tags to include with all events.
    /// </summary>
    public Dictionary<string, string> DefaultTags { get; } = [];

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required options are missing.</exception>
    public void Validate()
    {
        if (Enabled && string.IsNullOrWhiteSpace(DsnSecretName))
        {
            throw new InvalidOperationException("Sentry DSN secret name is required when Sentry sink is enabled.");
        }
    }
}
