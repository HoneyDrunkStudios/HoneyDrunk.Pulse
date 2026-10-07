// <copyright file="CollectorSmokeTests.cs" company="HoneyDrunk Studios">
// Copyright (c) HoneyDrunk Studios. All rights reserved.
// </copyright>

using AwesomeAssertions;
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Telemetry.Abstractions.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace HoneyDrunk.Pulse.Tests.Collector;

/// <summary>
/// Smoke tests for the Pulse Collector endpoints.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CollectorSmokeTests"/> class.
/// </remarks>
/// <param name="factory">The web application factory.</param>
[Collection(CollectorEnvVarCollection.CollectionName)]
public class CollectorSmokeTests(CollectorWebApplicationFactory factory) : IClassFixture<CollectorWebApplicationFactory>
{
    /// <summary>
    /// Verifies that the collector defaults to Kernel's canonical Pulse Node ID.
    /// </summary>
    [Fact]
    public void CollectorNodeContext_ShouldUseCanonicalPulseNodeIdFallback()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();

        // Act
        var nodeContext = scope.ServiceProvider.GetRequiredService<INodeContext>();

        // Assert
        nodeContext.NodeId.Should().Be(WellKnownNodes.Ops.Pulse.Value);
    }

    /// <summary>
    /// Verifies that deploy-time node ID configuration overrides the canonical fallback.
    /// </summary>
    [Fact]
    public void CollectorNodeContext_ShouldUseConfiguredNodeIdOverride()
    {
        // Arrange
        const string configuredNodeId = "honeydrunk-pulse-canary";
        using var overrideFactory = CollectorWebApplicationFactory.CreateWithNodeId(configuredNodeId);
        using var scope = overrideFactory.Services.CreateScope();

        // Act
        var nodeContext = scope.ServiceProvider.GetRequiredService<INodeContext>();

        // Assert
        nodeContext.NodeId.Should().Be(configuredNodeId);
    }

    /// <summary>
    /// Verifies that the health endpoint returns OK.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the ready endpoint returns OK.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ReadyEndpoint_ShouldReturnOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the live endpoint returns OK.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LiveEndpoint_ShouldReturnOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the traces endpoint accepts POST requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task TracesEndpoint_ShouldAcceptPost()
    {
        // Arrange
        var client = factory.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/traces", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the metrics endpoint accepts POST requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task MetricsEndpoint_ShouldAcceptPost()
    {
        // Arrange
        var client = factory.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/metrics", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the logs endpoint accepts POST requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LogsEndpoint_ShouldAcceptPost()
    {
        // Arrange
        var client = factory.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(new Uri("/otlp/v1/logs", UriKind.Relative), content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the analytics endpoint accepts valid requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AnalyticsEndpoint_ShouldAcceptValidRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new
        {
            Events = new[]
            {
                new
                {
                    EventName = "test.event",
                    DistinctId = "user-123",
                },
            },
            SourceService = "TestService",
        };

        // Act
        var response = await client.PostAsJsonAsync("/otlp/v1/analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that the analytics endpoint rejects empty events.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AnalyticsEndpoint_ShouldRejectEmptyEvents()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new
        {
            Events = Array.Empty<object>(),
        };

        // Act
        var response = await client.PostAsJsonAsync("/otlp/v1/analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that the errors endpoint accepts valid requests.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ErrorsEndpoint_ShouldAcceptValidRequest()
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new
        {
            Message = "Test error",
            CorrelationId = "corr-123",
        };

        // Act
        var response = await client.PostAsJsonAsync("/otlp/v1/errors", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Malformed payloads are client errors and never forwarded to configured sinks.</summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task TracesEndpoint_InvalidPayload_ReturnsBadRequestWithoutExport()
    {
        var sink = new CapturingTraceSink();
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<ITraceSink>(sink)));
        using var client = configuredFactory.CreateClient();
        using var body = new StringContent("{invalid-json", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(new Uri("/otlp/v1/traces", UriKind.Relative), body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        sink.ExportCount.Should().Be(0);
    }

    private sealed class CapturingTraceSink : ITraceSink
    {
        public int ExportCount { get; private set; }

        public Task ExportAsync(ReadOnlyMemory<byte> data, string contentType, CancellationToken cancellationToken = default)
        {
            ExportCount++;
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
