// <copyright file="IHttpOtlpSinkOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Shared;

/// <summary>
/// Shared HTTP OTLP sink option shape used by Grafana-family sinks.
/// </summary>
internal interface IHttpOtlpSinkOptions
{
    /// <summary>
    /// Gets the OTLP endpoint URL.
    /// </summary>
    string? Endpoint { get; }

    /// <summary>
    /// Gets a value indicating whether the sink is enabled.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Gets the HTTP client timeout in seconds.
    /// </summary>
    int TimeoutSeconds { get; }

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Gets optional custom headers to include in requests.
    /// </summary>
    Dictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the Vault secret name for the Authorization header or Basic auth value.
    /// </summary>
    string BasicAuthSecretName { get; }

    /// <summary>
    /// Gets the Vault secret name for the basic auth username.
    /// </summary>
    string UsernameSecretName { get; }

    /// <summary>
    /// Gets the Vault secret name for the basic auth password.
    /// </summary>
    string PasswordSecretName { get; }
}
