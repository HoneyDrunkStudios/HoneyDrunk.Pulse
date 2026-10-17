// <copyright file="HttpOtlpSinkExporter.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using System.Net.Http.Headers;
using System.Text;

namespace HoneyDrunk.Telemetry.Sink.Shared;

/// <summary>
/// Shared HTTP OTLP export/auth/retry behavior for Grafana-family sinks.
/// </summary>
internal static class HttpOtlpSinkExporter
{
    /// <summary>
    /// Exports OTLP payload data to the configured HTTP endpoint.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="secretStore">The Vault secret store.</param>
    /// <param name="options">The shared HTTP OTLP options.</param>
    /// <param name="payload">The OTLP payload.</param>
    /// <param name="contentType">The payload content type.</param>
    /// <param name="callbacks">Sink-specific logging callbacks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous export.</returns>
    public static async Task ExportAsync(
        HttpClient httpClient,
        ISecretStore secretStore,
        IHttpOtlpSinkOptions options,
        ReadOnlyMemory<byte> payload,
        string contentType,
        HttpOtlpSinkLogCallbacks callbacks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(secretStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(callbacks);

        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            callbacks.EndpointNotConfigured();
            return;
        }

        var maxRetries = Math.Max(1, options.MaxRetries);
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var content = new ByteArrayContent(payload.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.Endpoint, UriKind.Absolute))
                {
                    Content = content,
                };

                await ApplyRequestHeadersAsync(request, secretStore, options, cancellationToken)
                    .ConfigureAwait(false);

                using var response = await httpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    callbacks.Exported(payload.Length);
                    return;
                }

                var responseBody = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                callbacks.ExportFailed((int)response.StatusCode, responseBody);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries - 1)
            {
                callbacks.ExportRetry(attempt + 1, maxRetries, ex.Message);
                await Task.Delay(GetRetryDelay(attempt + 1), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private static async Task ApplyRequestHeadersAsync(
        HttpRequestMessage request,
        ISecretStore secretStore,
        IHttpOtlpSinkOptions options,
        CancellationToken cancellationToken)
    {
        foreach (var header in options.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var basicAuth = await TryGetSecretValueAsync(secretStore, options.BasicAuthSecretName, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(basicAuth))
        {
            request.Headers.Authorization = BuildBasicAuthHeader(basicAuth);
            return;
        }

        var username = await TryGetSecretValueAsync(secretStore, options.UsernameSecretName, cancellationToken)
            .ConfigureAwait(false);
        var password = await TryGetSecretValueAsync(secretStore, options.PasswordSecretName, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            request.Headers.Authorization = BuildBasicAuthHeader($"{username}:{password}");
        }
    }

    private static async Task<string?> TryGetSecretValueAsync(
        ISecretStore secretStore,
        string secretName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            return null;
        }

        var result = await secretStore
            .TryGetSecretAsync(new SecretIdentifier(secretName), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? result.Value?.Value : null;
    }

    private static AuthenticationHeaderValue BuildBasicAuthHeader(string value)
    {
        if (AuthenticationHeaderValue.TryParse(value, out var parsed)
            && !string.IsNullOrWhiteSpace(parsed.Scheme))
        {
            return parsed;
        }

        var parameter = value.Contains(':', StringComparison.Ordinal)
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            : value;

        return new AuthenticationHeaderValue("Basic", parameter);
    }

    private static TimeSpan GetRetryDelay(int attempt) =>
        TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));
}
