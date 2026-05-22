// <copyright file="HttpOtlpSinkExporterTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Telemetry.Sink.Loki.Implementation;
using HoneyDrunk.Telemetry.Sink.Loki.Options;
using HoneyDrunk.Telemetry.Sink.Mimir.Implementation;
using HoneyDrunk.Telemetry.Sink.Mimir.Options;
using HoneyDrunk.Telemetry.Sink.Tempo.Implementation;
using HoneyDrunk.Telemetry.Sink.Tempo.Options;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace HoneyDrunk.Pulse.Tests.Telemetry;

/// <summary>
/// Verifies shared HTTP OTLP sink export behavior through concrete sinks.
/// </summary>
public class HttpOtlpSinkExporterTests
{
    /// <summary>
    /// Verifies configured headers and username/password secrets become a Basic auth header.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LokiExportAsync_ShouldApplyHeadersAndUsernamePasswordBasicAuth()
    {
        // Arrange
        using var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        using var httpClient = new HttpClient(handler);
        var secrets = new StubSecretStore(new Dictionary<string, string>
        {
            ["Loki--Username"] = "tenant",
            ["Loki--Password"] = "password",
        });
        using var sink = new LokiSink(
            httpClient,
            secrets,
            Options.Create(new LokiSinkOptions
            {
                Endpoint = "https://loki.example/otlp/v1/logs",
                Headers = { ["X-Scope-OrgID"] = "honeydrunk" },
            }),
            NullLogger<LokiSink>.Instance);

        // Act
        await sink.ExportAsync(Encoding.UTF8.GetBytes("logs"), "application/json");

        // Assert
        handler.Requests.Should().ContainSingle();
        var request = handler.Requests[0];
        request.Headers.GetValues("X-Scope-OrgID").Should().ContainSingle("honeydrunk");
        request.Headers.Authorization.Should().BeEquivalentTo(
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("tenant:password"))));
    }

    /// <summary>
    /// Verifies HTTP request failures are retried by the shared exporter.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task MimirExportAsync_ShouldRetryTransientHttpRequestFailures()
    {
        // Arrange
        var attempts = 0;
        using var handler = new CapturingHandler(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new HttpRequestException("transient");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler);
        using var sink = new MimirSink(
            httpClient,
            new StubSecretStore(),
            Options.Create(new MimirSinkOptions
            {
                Endpoint = "https://mimir.example/otlp/v1/metrics",
                MaxRetries = 2,
            }),
            NullLogger<MimirSink>.Instance);

        // Act
        await sink.ExportAsync(Encoding.UTF8.GetBytes("metrics"), "application/json");

        // Assert
        attempts.Should().Be(2);
        handler.Requests.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies a full Authorization header secret is preserved when supplied.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TempoExportAsync_ShouldPreserveAuthorizationHeaderSecret()
    {
        // Arrange
        using var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        using var sink = new TempoSink(
            httpClient,
            new StubSecretStore(new Dictionary<string, string>
            {
                ["Tempo--BasicAuth"] = "Bearer tempo-token",
            }),
            Options.Create(new TempoSinkOptions
            {
                Endpoint = "https://tempo.example/v1/traces",
            }),
            NullLogger<TempoSink>.Instance);

        // Act
        await sink.ExportAsync(Encoding.UTF8.GetBytes("traces"), "application/json");

        // Assert
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].Headers.Authorization.Should().BeEquivalentTo(
            new AuthenticationHeaderValue("Bearer", "tempo-token"));
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            Requests.Add(clone);
            return Task.FromResult(responder(request));
        }
    }

    private sealed class StubSecretStore(IReadOnlyDictionary<string, string>? secrets = null) : ISecretStore
    {
        private readonly IReadOnlyDictionary<string, string> _secrets = secrets ?? new Dictionary<string, string>();

        public Task<SecretValue> GetSecretAsync(
            SecretIdentifier identifier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SecretValue(identifier, _secrets[identifier.Name], version: null));

        public Task<VaultResult<SecretValue>> TryGetSecretAsync(
            SecretIdentifier identifier,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_secrets.TryGetValue(identifier.Name, out var value)
                ? VaultResult.Success(new SecretValue(identifier, value, version: null))
                : VaultResult.Failure<SecretValue>($"Secret '{identifier.Name}' was not found."));
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
            string secretName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SecretVersion>>([]);
    }
}
