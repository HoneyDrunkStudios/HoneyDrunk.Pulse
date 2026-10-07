// <copyright file="OtlpAuthenticationTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Pulse.Collector.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Exercises the actual host's HTTP and gRPC ingestion authentication boundary.
/// </summary>
/// <param name="factory">The authenticated collector fixture.</param>
[Collection(CollectorEnvVarCollection.CollectionName)]
public class OtlpAuthenticationTests(AuthenticatedCollectorWebApplicationFactory factory)
    : IClassFixture<AuthenticatedCollectorWebApplicationFactory>
{
    /// <summary>
    /// Missing or malformed tokens are rejected before any ingestion endpoint parses the body.
    /// </summary>
    /// <param name="path">The ingestion endpoint.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData("/otlp/v1/traces")]
    [InlineData("/otlp/v1/metrics")]
    [InlineData("/otlp/v1/logs")]
    [InlineData("/otlp/v1/analytics")]
    [InlineData("/otlp/v1/errors")]
    [InlineData("/opentelemetry.proto.collector.trace.v1.TraceService/Export")]
    [InlineData("/opentelemetry.proto.collector.metrics.v1.MetricsService/Export")]
    [InlineData("/opentelemetry.proto.collector.logs.v1.LogsService/Export")]
    public async Task IngestionEndpoint_MissingOrMalformedToken_ReturnsUnauthorized(string path)
    {
        using var client = factory.CreateClient();
        foreach (var token in new[] { null, "not-a-jwt" })
        {
            using var request = CreateRequest(path, token);
            using var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            response.Headers.WwwAuthenticate.Should().Contain(challenge => challenge.Scheme == "Bearer");
        }
    }

    /// <summary>
    /// Properly issued tokens reach both HTTP handlers and real gRPC services.
    /// </summary>
    /// <param name="path">The ingestion endpoint.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData("/otlp/v1/traces")]
    [InlineData("/otlp/v1/metrics")]
    [InlineData("/otlp/v1/logs")]
    [InlineData("/otlp/v1/analytics")]
    [InlineData("/otlp/v1/errors")]
    [InlineData("/opentelemetry.proto.collector.trace.v1.TraceService/Export")]
    [InlineData("/opentelemetry.proto.collector.metrics.v1.MetricsService/Export")]
    [InlineData("/opentelemetry.proto.collector.logs.v1.LogsService/Export")]
    public async Task IngestionEndpoint_ValidToken_AcceptsRequest(string path)
    {
        using var client = factory.CreateClient();
        using var request = CreateRequest(path, factory.CreateAccessToken());

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        if (path.EndsWith("/Export", StringComparison.Ordinal))
        {
            await response.Content.ReadAsByteArrayAsync();
            response.TrailingHeaders.GetValues("grpc-status").Should().ContainSingle().Which.Should().Be("0");
        }
    }

    /// <summary>
    /// A signed token cannot bypass issuer, audience, lifetime, or signing-key validation.
    /// </summary>
    /// <param name="path">The representative HTTP or gRPC route.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData("/otlp/v1/traces")]
    [InlineData("/opentelemetry.proto.collector.trace.v1.TraceService/Export")]
    public async Task IngestionEndpoint_InvalidTokenClaimsOrSignature_ReturnsUnauthorized(string path)
    {
        using var client = factory.CreateClient();
        var tokens = new[]
        {
            factory.CreateAccessToken(audience: "unrelated-api"),
            factory.CreateAccessToken(issuer: "https://untrusted.example.com"),
            factory.CreateAccessToken(expires: DateTime.UtcNow.AddMinutes(-2)),
            factory.CreateAccessToken(useUntrustedKey: true),
        };

        foreach (var token in tokens)
        {
            using var request = CreateRequest(path, token);
            using var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>
    /// All ingestion routes share the policy while probes and the Vault webhook remain outside it.
    /// </summary>
    [Fact]
    public void IngestionPolicy_IsScopedToAllEightIngestionEndpoints()
    {
        factory.Services.GetRequiredService<IOptions<PulseCollectorOptions>>().Value
            .RequireOtlpAuthentication.Should().BeTrue();
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
        var ingestionRoutes = routes.Where(route =>
            route.RoutePattern.RawText != null
            && (route.RoutePattern.RawText.StartsWith("/otlp/v1/", StringComparison.Ordinal)
                || route.RoutePattern.RawText.EndsWith("/Export", StringComparison.Ordinal))).ToList();

        ingestionRoutes.Should().HaveCount(8);
        ingestionRoutes.Should().OnlyContain(route => route.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(data => data.Policy == OtlpAuthenticationExtensions.PolicyName));
        var webhook = routes.Single(route => route.RoutePattern.RawText == "/internal/vault/invalidate");
        webhook.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Should().NotContain(data => data.Policy == OtlpAuthenticationExtensions.PolicyName);
    }

    /// <summary>
    /// Authentication does not block platform liveness and readiness probes.
    /// </summary>
    /// <param name="path">The health route.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_WithoutToken_RemainsAccessible(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpRequestMessage CreateRequest(string path, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (path.EndsWith("/Export", StringComparison.Ordinal))
        {
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            request.Content = new ByteArrayContent([0, 0, 0, 0, 0]);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
            request.Headers.TE.Add(new TransferCodingWithQualityHeaderValue("trailers"));
        }
        else
        {
            var body = path switch
            {
                "/otlp/v1/analytics" => "{\"events\":[{\"eventName\":\"auth.test\",\"distinctId\":\"test-client\"}]}",
                "/otlp/v1/errors" => "{\"message\":\"Authentication test\"}",
                _ => "{}",
            };
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return request;
    }
}
