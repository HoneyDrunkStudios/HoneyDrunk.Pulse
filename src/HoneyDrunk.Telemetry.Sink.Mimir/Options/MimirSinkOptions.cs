// <copyright file="MimirSinkOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Mimir.Options;

/// <summary>
/// Configuration options for the Mimir metrics sink.
/// </summary>
public sealed class MimirSinkOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:Mimir";

    /// <summary>
    /// Gets or sets the Mimir OTLP endpoint URL.
    /// </summary>
    /// <remarks>
    /// For Mimir with OTLP support, this is typically the OTLP HTTP receiver endpoint,
    /// e.g., "http://mimir:4318/otlp/v1/metrics".
    /// </remarks>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the protocol to use (Http or Grpc).
    /// </summary>
    public MimirProtocol Protocol { get; set; } = MimirProtocol.Http;

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
    /// Gets or sets the Vault secret name for the Mimir Authorization header or Basic auth value.
    /// </summary>
    public string BasicAuthSecretName { get; set; } = "Mimir--BasicAuth";

    /// <summary>
    /// Gets or sets the Vault secret name for the Mimir basic auth username.
    /// </summary>
    public string UsernameSecretName { get; set; } = "Mimir--Username";

    /// <summary>
    /// Gets or sets the Vault secret name for the Mimir basic auth password.
    /// </summary>
    public string PasswordSecretName { get; set; } = "Mimir--Password";
}
