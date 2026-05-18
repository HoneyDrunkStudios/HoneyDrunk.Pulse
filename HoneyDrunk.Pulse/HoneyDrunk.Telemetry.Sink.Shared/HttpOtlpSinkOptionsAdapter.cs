// <copyright file="HttpOtlpSinkOptionsAdapter.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Shared;

/// <summary>
/// Internal adapter for shared HTTP OTLP sink option values.
/// </summary>
internal sealed class HttpOtlpSinkOptionsAdapter : IHttpOtlpSinkOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpOtlpSinkOptionsAdapter"/> class.
    /// </summary>
    /// <param name="endpoint">The OTLP endpoint URL.</param>
    /// <param name="enabled">A value indicating whether the sink is enabled.</param>
    /// <param name="timeoutSeconds">The HTTP client timeout in seconds.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <param name="headers">Optional custom headers to include in requests.</param>
    /// <param name="basicAuthSecretName">The Vault secret name for an Authorization header or Basic auth value.</param>
    /// <param name="usernameSecretName">The Vault secret name for the basic auth username.</param>
    /// <param name="passwordSecretName">The Vault secret name for the basic auth password.</param>
    public HttpOtlpSinkOptionsAdapter(
        string? endpoint,
        bool enabled,
        int timeoutSeconds,
        int maxRetries,
        Dictionary<string, string> headers,
        string basicAuthSecretName,
        string usernameSecretName,
        string passwordSecretName)
    {
        Endpoint = endpoint;
        Enabled = enabled;
        TimeoutSeconds = timeoutSeconds;
        MaxRetries = maxRetries;
        Headers = headers;
        BasicAuthSecretName = basicAuthSecretName;
        UsernameSecretName = usernameSecretName;
        PasswordSecretName = passwordSecretName;
    }

    /// <inheritdoc />
    public string? Endpoint { get; }

    /// <inheritdoc />
    public bool Enabled { get; }

    /// <inheritdoc />
    public int TimeoutSeconds { get; }

    /// <inheritdoc />
    public int MaxRetries { get; }

    /// <inheritdoc />
    public Dictionary<string, string> Headers { get; }

    /// <inheritdoc />
    public string BasicAuthSecretName { get; }

    /// <inheritdoc />
    public string UsernameSecretName { get; }

    /// <inheritdoc />
    public string PasswordSecretName { get; }
}
