// <copyright file="MimirSink.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using HoneyDrunk.Telemetry.Sink.Mimir.Options;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

namespace HoneyDrunk.Telemetry.Sink.Mimir.Implementation;

/// <summary>
/// HTTP-based Mimir metrics sink implementation that forwards OTLP metric data to Mimir.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MimirSink"/> class.
/// </remarks>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="secretStore">The Vault secret store.</param>
/// <param name="options">The Mimir sink options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class MimirSink(
    HttpClient httpClient,
    ISecretStore secretStore,
    IOptions<MimirSinkOptions> options,
    ILogger<MimirSink> logger) : IMetricsSink, IDisposable
{
    private readonly MimirSinkOptions _options = options.Value;
    private bool _disposed;

    /// <inheritdoc />
    public async Task ExportAsync(
        ReadOnlyMemory<byte> metricData,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrEmpty(_options.Endpoint))
        {
            LogEndpointNotConfigured();
            return;
        }

        var attempt = 0;
        while (attempt < _options.MaxRetries)
        {
            try
            {
                using var content = new ByteArrayContent(metricData.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.Endpoint!))
                {
                    Content = content,
                };

                await ApplyRequestHeadersAsync(request, cancellationToken).ConfigureAwait(false);

                using var response = await httpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    LogMetricsExported(metricData.Length);
                    return;
                }

                var responseBody = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                LogExportFailed((int)response.StatusCode, responseBody);
                attempt++;
            }
            catch (HttpRequestException ex) when (attempt < _options.MaxRetries - 1)
            {
                LogExportRetry(attempt + 1, _options.MaxRetries, ex.Message);
                attempt++;
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // HTTP exports are immediate, no buffering to flush
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Exported {ByteCount} bytes of metric data to Mimir")]
    private partial void LogMetricsExported(int byteCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Failed to export metrics to Mimir. Status: {StatusCode}, Response: {Response}")]
    private partial void LogExportFailed(int statusCode, string response);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Retrying Mimir export (attempt {Attempt}/{MaxRetries}): {Error}")]
    private partial void LogExportRetry(int attempt, int maxRetries, string error);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Mimir endpoint not configured, skipping export")]
    private partial void LogEndpointNotConfigured();

    private async Task ApplyRequestHeadersAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        foreach (var header in _options.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var basicAuth = await TryGetSecretValueAsync(_options.BasicAuthSecretName, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(basicAuth))
        {
            request.Headers.Authorization = BuildBasicAuthHeader(basicAuth);
            return;
        }

        var username = await TryGetSecretValueAsync(_options.UsernameSecretName, cancellationToken)
            .ConfigureAwait(false);
        var password = await TryGetSecretValueAsync(_options.PasswordSecretName, cancellationToken)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            request.Headers.Authorization = BuildBasicAuthHeader($"{username}:{password}");
        }
    }

    private async Task<string?> TryGetSecretValueAsync(string secretName, CancellationToken cancellationToken)
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1204:Static elements should appear before instance elements",
        Justification = "Helper kept adjacent to its only caller TryGetSecretValueAsync for readability.")]
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
}
