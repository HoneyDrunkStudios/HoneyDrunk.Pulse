// <copyright file="PostHogSinkOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.PostHog.Options;

/// <summary>
/// Configuration options for the PostHog analytics sink.
/// </summary>
public sealed class PostHogSinkOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:PostHog";

    /// <summary>
    /// Gets or sets the Vault secret name for the PostHog API key.
    /// </summary>
    public string ApiKeySecretName { get; set; } = "PostHog--ApiKey";

    /// <summary>
    /// Gets or sets the PostHog host URL.
    /// </summary>
    public string Host { get; set; } = "https://app.posthog.com";

    /// <summary>
    /// Gets or sets a value indicating whether the sink is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for sending events.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the flush interval in milliseconds.
    /// </summary>
    public int FlushIntervalMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the HTTP client timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets a list of property keys that should be excluded from events.
    /// </summary>
    public List<string> ExcludedPropertyKeys { get; } = [];

    /// <summary>
    /// Gets a list of property keys that are approved for inclusion.
    /// If empty, all non-excluded keys are included.
    /// </summary>
    public List<string> ApprovedPropertyKeys { get; } = [];

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required options are missing.</exception>
    public void Validate()
    {
        if (Enabled && string.IsNullOrWhiteSpace(ApiKeySecretName))
        {
            throw new InvalidOperationException("PostHog API key secret name is required when PostHog sink is enabled.");
        }
    }
}
