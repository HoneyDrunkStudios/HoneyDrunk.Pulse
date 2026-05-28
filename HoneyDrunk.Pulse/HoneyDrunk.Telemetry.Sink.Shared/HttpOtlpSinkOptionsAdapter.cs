// <copyright file="HttpOtlpSinkOptionsAdapter.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

namespace HoneyDrunk.Telemetry.Sink.Shared;

/// <summary>
/// Internal adapter for shared HTTP OTLP sink option values.
/// </summary>
/// <param name="endpoint">The OTLP endpoint URL.</param>
/// <param name="enabled">A value indicating whether the sink is enabled.</param>
/// <param name="timeoutSeconds">The HTTP client timeout in seconds.</param>
/// <param name="maxRetries">The maximum number of retry attempts.</param>
/// <param name="headers">Optional custom headers to include in requests.</param>
/// <param name="authSecretNames">Bundle of Vault secret names that resolve HTTP authentication material.</param>
internal sealed class HttpOtlpSinkOptionsAdapter(
    string? endpoint,
    bool enabled,
    int timeoutSeconds,
    int maxRetries,
    Dictionary<string, string> headers,
    HttpOtlpSinkAuthSecretNames authSecretNames) : IHttpOtlpSinkOptions
{
    /// <inheritdoc />
    public string? Endpoint { get; } = endpoint;

    /// <inheritdoc />
    public bool Enabled { get; } = enabled;

    /// <inheritdoc />
    public int TimeoutSeconds { get; } = timeoutSeconds;

    /// <inheritdoc />
    public int MaxRetries { get; } = maxRetries;

    /// <inheritdoc />
    public Dictionary<string, string> Headers { get; } = headers;

    /// <inheritdoc />
    public string BasicAuthSecretName { get; } = authSecretNames.BasicAuthSecretName;

    /// <inheritdoc />
    public string UsernameSecretName { get; } = authSecretNames.UsernameSecretName;

    /// <inheritdoc />
    public string PasswordSecretName { get; } = authSecretNames.PasswordSecretName;
}
