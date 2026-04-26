// <copyright file="TempoSinkOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Tempo.Options;

/// <summary>
/// Configuration options for the Tempo trace sink.
/// </summary>
public sealed class TempoSinkOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:Tempo";

    /// <summary>
    /// Gets or sets the Tempo OTLP endpoint URL.
    /// </summary>
    /// <remarks>
    /// For Tempo, this is typically the OTLP HTTP receiver endpoint,
    /// e.g., "http://tempo:4318/v1/traces".
    /// </remarks>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the protocol to use (Http or Grpc).
    /// </summary>
    public TempoProtocol Protocol { get; set; } = TempoProtocol.Http;

    /// <summary>
    /// Gets or sets a value indicating whether the sink is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP client timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets optional custom headers to include in requests.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = [];

    /// <summary>
    /// Gets or sets the Vault secret name for the Tempo Authorization header or Basic auth value.
    /// </summary>
    public string BasicAuthSecretName { get; set; } = "Tempo--BasicAuth";

    /// <summary>
    /// Gets or sets the Vault secret name for the Tempo basic auth username.
    /// </summary>
    public string UsernameSecretName { get; set; } = "Tempo--Username";

    /// <summary>
    /// Gets or sets the Vault secret name for the Tempo basic auth password.
    /// </summary>
    public string PasswordSecretName { get; set; } = "Tempo--Password";
}
