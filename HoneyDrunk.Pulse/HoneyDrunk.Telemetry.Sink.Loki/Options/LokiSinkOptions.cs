// <copyright file="LokiSinkOptions.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Telemetry.Sink.Loki.Options;

/// <summary>
/// Configuration options for the Loki log sink.
/// </summary>
public sealed class LokiSinkOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HoneyDrunk:Loki";

    /// <summary>
    /// Gets or sets the Loki OTLP endpoint URL.
    /// </summary>
    /// <remarks>
    /// For Loki with OTLP support, this is typically the OTLP HTTP receiver endpoint,
    /// e.g., "http://loki:3100/otlp/v1/logs".
    /// </remarks>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the protocol to use (Http or Grpc).
    /// </summary>
    public LokiProtocol Protocol { get; set; } = LokiProtocol.Http;

    /// <summary>
    /// Gets or sets a value indicating whether the sink is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level to export.
    /// </summary>
    /// <remarks>
    /// Default is Warning for production environments to reduce log volume.
    /// </remarks>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Warning;

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
    /// Gets or sets the Vault secret name for the Loki Authorization header or Basic auth value.
    /// </summary>
    public string BasicAuthSecretName { get; set; } = "Loki--BasicAuth";

    /// <summary>
    /// Gets or sets the Vault secret name for the Loki basic auth username.
    /// </summary>
    public string UsernameSecretName { get; set; } = "Loki--Username";

    /// <summary>
    /// Gets or sets the Vault secret name for the Loki basic auth password.
    /// </summary>
    public string PasswordSecretName { get; set; } = "Loki--Password";
}
